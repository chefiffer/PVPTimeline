using System.Diagnostics;
using System.Numerics;
using System.Threading.Channels;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using PvpSkillLab;
using GameAction = Lumina.Excel.Sheets.Action;

namespace PvpTimeline;

/// <summary>独立显示插件：不引用/读取采集器DLL或日志，不写盘，不上传。Hook只复制有限的本机事件到有界队列。</summary>
public sealed unsafe class Plugin : IDalamudPlugin
{
    [PluginService] private static IDalamudPluginInterface Pi { get; set; } = null!;
    [PluginService] private static IFramework Framework { get; set; } = null!;
    [PluginService] private static IObjectTable Objects { get; set; } = null!;
    [PluginService] private static IDataManager Data { get; set; } = null!;
    [PluginService] private static IGameInteropProvider Interop { get; set; } = null!;
    [PluginService] private static ICommandManager Commands { get; set; } = null!;
    [PluginService] private static IPluginLog Log { get; set; } = null!;
    [PluginService] private static IClientState ClientState { get; set; } = null!;
    [PluginService] private static ICondition Condition { get; set; } = null!;
    [PluginService] private static ITextureProvider Textures { get; set; } = null!;
    private delegate bool UseAction(ActionManager* manager, ActionType type, uint id, ulong target, uint extra, ActionManager.UseActionMode mode, uint route, bool* area);
    private delegate void ReceiveEffect(uint casterId, Character* caster, Vector3* position, ActionEffectHandler.Header* header, ActionEffectHandler.TargetEffects* effects, GameObjectId* targets);
    private Hook<UseAction>? requestHook;
    private Hook<ReceiveEffect>? effectHook;
    private readonly NativeAnimationObserver animationObserver;
    private readonly record struct AnimationMessage(SelfAnimationSignal? Signal, AnimationPlayRequest? Request);
    private readonly Channel<AnimationMessage> animationQueue = Channel.CreateBounded<AnimationMessage>(new BoundedChannelOptions(512) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });
    private readonly record struct Observation(byte Kind, double Time, uint Action, ulong Target, ushort Sequence = 0, uint Global = 0,
        float Lock = 0, byte EffectType = 0, uint Value = 0, bool Queued = false, float Duration = 0, byte DamageFlags = 0);
    private readonly Channel<Observation> queue = Channel.CreateBounded<Observation>(new BoundedChannelOptions(512) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });
    private readonly Dictionary<uint, ActionInfo> actions = new();
    private readonly TimelineModel model = new();
    private readonly Configuration config;
    private readonly TimelineWindow window;
    private readonly BuffState buffs=new();
    private readonly Dictionary<uint,BuffInfo> buffMetadata=new();
    private double nextBuffSample;
    private TimelineEntry? casting;
    private bool settingsVisible;
    private bool capturing;
    private bool frozen;
    private bool observationFault;
    private double frozenTime;
    private uint localId;
    private nint localAddress;
    private uint territory = uint.MaxValue;
    private uint duty;
    private readonly PvpAreaResolver areaResolver = new();
    private int areaInvalidationPending = 1;
    private bool wasLoggedIn;
    // Cache the callback once; passing it through the frame path allocates nothing.
    private static readonly Func<uint> dutyReader = ReadCurrentDuty;
    private long dropped;
    private string hookState = "初始化 / Initializing";
    private static double Now => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;

    public Plugin()
    {
        config = Pi.GetPluginConfig() as Configuration ?? new Configuration();
        if(config.Language<0)config.Language=UiText.DefaultLanguage(Pi.UiLanguage);
        if (config.Version < 2)
        {
            config.ShowLegend = false; config.ShowCooldown = false; config.LineThickness = 6;
            config.Version = 2; Pi.SavePluginConfig(config);
        }
        if (config.Version < 3) { config.Version = 3; Pi.SavePluginConfig(config); }
        // Preserve the old shared offset once, then edit the two tracks independently.
        if(config.Version<4){config.AbilityBarOffset=config.GcdBarOffset;config.Version=4;Pi.SavePluginConfig(config);}
        if(config.Version<5){config.AbilityLineThickness=config.LineThickness;config.Version=5;Pi.SavePluginConfig(config);}
        config.Normalize();
        var translationState=UiText.LoadOverride(Pi.AssemblyLocation.DirectoryName);
        Log.Information("PvP Timeline translations: {State}",translationState);
        window = new TimelineWindow(config, model, Textures, () => settingsVisible = true);
        window.InitializeGlow(Data);
        Commands.AddHandler("/pvptimeline", new CommandInfo((_, _) => settingsVisible = !settingsVisible) { HelpMessage = "PvP timeline settings / PvP时间轴设置" });
        try { requestHook = Interop.HookFromSignature<UseAction>("E8 ?? ?? ?? ?? B0 01 EB B6", OnUse); requestHook.Enable(); }
        catch (Exception e) { Log.Warning(e, "PvP Timeline request hook unavailable"); }
        try { effectHook = Interop.HookFromSignature<ReceiveEffect>(ActionEffectHandler.Addresses.Receive.String, OnEffect); effectHook.Enable(); }
        catch (Exception e) { Log.Warning(e, "PvP Timeline effect hook unavailable"); }
        animationObserver = new(Interop, () => Volatile.Read(ref localAddress), () => Volatile.Read(ref capturing) && Volatile.Read(ref config.ObserveNativeAnimations),
            signal => { if (!animationQueue.Writer.TryWrite(new(signal, null))) Interlocked.Increment(ref dropped); },
            request => { if (!animationQueue.Writer.TryWrite(new(null, request))) Interlocked.Increment(ref dropped); }, Log);
        hookState = $"request={requestHook?.IsEnabled == true}; effect={effectHook?.IsEnabled == true}; native animation={animationObserver.PlayEnabled}";
        Framework.Update += Update; Pi.UiBuilder.Draw += Draw;
        ClientState.TerritoryChanged += OnAreaChanged;
        ClientState.Login += OnLogin;
        Pi.UiBuilder.OpenConfigUi += OpenSettings;
    }
    private void OpenSettings() => settingsVisible = true;
    private void OnAreaChanged(uint _) => InvalidateArea();
    private void OnLogin() => InvalidateArea();
    private void InvalidateArea()
    {
        Volatile.Write(ref capturing,false);
        Interlocked.Exchange(ref areaInvalidationPending,1);
    }
    private static uint ReadCurrentDuty()
    {
        var gameMain=GameMain.Instance();
        return gameMain==null?0u:(uint)gameMain->CurrentContentFinderConditionId;
    }
    private void Enqueue(Observation value) { if (!queue.Writer.TryWrite(value)) Interlocked.Increment(ref dropped); }
    private bool OnUse(ActionManager* manager, ActionType type, uint id, ulong target, uint extra, ActionManager.UseActionMode mode, uint route, bool* area)
    {
        var before = Now;
        var adjusted = id;
        try { if (Volatile.Read(ref capturing) && type == ActionType.Action && manager != null) adjusted = manager->GetAdjustedActionId(id); }
        catch { /* 调整ID读取失败仍必须调用游戏原函数。 */ }
        using var animationContext = new NativeAnimationObserver.ActionScope(Volatile.Read(ref capturing) && type == ActionType.Action ? adjusted : 0);
        var accepted = requestHook!.Original(manager, type, id, target, extra, mode, route, area);
        try
        {
            if (Volatile.Read(ref capturing) && accepted && type == ActionType.Action)
            {
                Enqueue(new(0, before, adjusted, target, Queued: manager != null && manager->ActionQueued));
                if (accepted && manager != null)
                {
                    var gcd = manager->GetRecastGroupDetail(57);
                    if (gcd != null && gcd->IsActive) Enqueue(new(3, Now, gcd->ActionId, 0, Lock: gcd->Elapsed, Duration: gcd->Total));
                }
            }
        }
        catch (Exception e) { Log.Verbose(e, "PvP Timeline request observation skipped"); }
        return accepted;
    }
    private void OnEffect(uint casterId, Character* caster, Vector3* position, ActionEffectHandler.Header* header, ActionEffectHandler.TargetEffects* effects, GameObjectId* targets)
    {
        var received = Now;
        effectHook!.Original(casterId, caster, position, header, effects, targets);
        try
        {
            if (!Volatile.Read(ref capturing) || casterId != Volatile.Read(ref localId) || header == null || effects == null || targets == null) return;
            var action = (uint)header->SpellId;
            var sequence = header->SourceSequence;
            var global = header->GlobalSequence;
            // Mount casts use Mount-sheet IDs, while completion may use SpellId 4.
            // Classify before enqueueing an acknowledgement or it creates a second
            // unknown battle-action icon. The bounded scan does not retain pointers.
            var mountResult=(ActionType)header->ActionType==ActionType.Mount;
            if(action==4 && !mountResult)
                for(var i=0;i<Math.Min(64,(int)header->NumTargets) && !mountResult;i++)
                    for(var slot=0;slot<8;slot++)
                        if(MountObservation.IsMountResult(header->ActionType,action,effects[i].Effects[slot].Type)){mountResult=true;break;}
            if(mountResult)
            { Enqueue(new(5,received,MountObservation.ActionId,header->NumTargets>0?(ulong)targets[0]:casterId,sequence,global,header->AnimationLock));return; }
            var hasRows=false;
            if (sequence != 0) Enqueue(new(1, received, action, header->NumTargets > 0 ? (ulong)targets[0] : 0, sequence, global, header->AnimationLock));
            for (var i = 0; i < Math.Min(64, (int)header->NumTargets); i++)
                for (var slot = 0; slot < 8; slot++)
                {
                    ref var e = ref effects[i].Effects[slot];
                    if (!TimelineModel.IsObservableEffect(e.Type)) continue;
                    hasRows=true;
                    var value = (uint)e.Value + ((e.Type == 3 && (e.Param4 & 0x40) != 0) ? (uint)e.Param3 << 16 : 0);
                    Enqueue(new(2, received, action, (uint)(targets[i] & uint.MaxValue), sequence, global, EffectType: e.Type, Value: value,
                        DamageFlags: e.Type is 3 or 4 or 5 or 6 ? (byte)(e.Param0 & 0x60) : (byte)0));
                }
            if(sequence==0 && !hasRows && TimelinePresentation.MovementReceipt(action))Enqueue(new(4,received,action,0));
        }
        catch (Exception e) { Log.Verbose(e, "PvP Timeline effect observation skipped"); }
    }
    private ActionInfo? Info(uint id)
    {
        if (actions.TryGetValue(id, out var value)) return value;
        if(id==MountObservation.ActionId)return actions[id]=new(id,UiText.T("Mounting"),MountObservation.Icon,0,0,0,0,0,0,0);
        if (!Data.GetExcelSheet<GameAction>(NameLanguage()).TryGetRow(id, out var a)) return null;
        value = new(id, UiText.GameName("actions",id,config.Language,a.Name.ToString()), a.Icon, a.ActionCategory.RowId, a.CooldownGroup, a.AdditionalCooldownGroup,
            a.Cast100ms / 10d, a.Recast100ms / 10d, a.AnimationStart.RowId, a.AnimationEnd.RowId);
        // 已核实的内部行可能只有405占位图；展示对应技能图标，原始ID仍保留。
        if(TimelinePresentation.DisplayAction(id)!=id && Data.GetExcelSheet<GameAction>(NameLanguage()).TryGetRow(TimelinePresentation.DisplayAction(id),out var display))
            value=value with {Icon=display.Icon,Name=UiText.GameName("actions",display.RowId,config.Language,display.Name.ToString()),Category=display.ActionCategory.RowId,PrimaryGroup=0,AdditionalGroup=0};
        if (actions.Count >= 1024) actions.Remove(actions.Keys.First());
        actions[id] = value; return value;
    }
    private BuffInfo? GetBuffInfo(uint id)
    {
        if(buffMetadata.TryGetValue(id,out var cached))return cached;
        if(!Data.GetExcelSheet<Lumina.Excel.Sheets.Status>(NameLanguage()).TryGetRow(id,out var row))return null;
        if(buffMetadata.Count>=256)buffMetadata.Remove(buffMetadata.Keys.First());
        return buffMetadata[id]=new(id,UiText.GameName("statuses",id,config.Language,row.Name.ToString()),row.Description.ToString(),row.Icon,row.StatusCategory==2);
    }
    private Dalamud.Game.ClientLanguage? NameLanguage()=>config.Language switch {2=>Dalamud.Game.ClientLanguage.English,3=>Dalamud.Game.ClientLanguage.German,4=>Dalamud.Game.ClientLanguage.French,5=>Dalamud.Game.ClientLanguage.Japanese,_=>null};
    private void Update(IFramework framework)
    {
        try
        {
            var player = Objects.LocalPlayer;
            var loggedIn = ClientState.IsLoggedIn;
            Volatile.Write(ref localAddress, player?.Address ?? 0);
            if (Interlocked.Exchange(ref areaInvalidationPending,0)!=0 || territory != ClientState.TerritoryType ||
                localId != (player?.EntityId ?? 0) || wasLoggedIn != loggedIn)
            {
                areaResolver.Invalidate();duty=0;wasLoggedIn=loggedIn;
                territory = ClientState.TerritoryType; Volatile.Write(ref localId, player?.EntityId ?? 0);
                animationObserver.Reset("map_or_character_changed");
                model.Clear(); casting = null; frozen = false; observationFault = false;
                buffs.Clear();nextBuffSample=0;
                while (animationQueue.Reader.TryRead(out _)) { }
                while (queue.Reader.TryRead(out _)) { }
            }
            // Events wake resolution; only unresolved, ready instances query at
            // 200ms intervals. Once resolved, the native reader is not called.
            var areaReady=loggedIn && player!=null && !Condition[ConditionFlag.BetweenAreas] && !Condition[ConditionFlag.BetweenAreas51];
            areaResolver.Update(Now,territory,areaReady,dutyReader);
            duty=areaResolver.Duty;
            Volatile.Write(ref capturing, !observationFault && config.Enabled && areaReady && areaResolver.Eligible);
            if (!capturing)
            {
                animationObserver.Reset("observation_paused");
                while (animationQueue.Reader.TryRead(out var tail)) if (tail.Signal != null) model.NativeAnimation(tail.Signal);
                while (queue.Reader.TryRead(out _)) { } return;
            }
            var manager = ActionManager.Instance();
            // UI冻结只冻结观察时钟，底层有界记录仍继续，恢复后可继续看实时数据。
            var now = Now;
            if(config.ShowOwnBuffs && now>=nextBuffSample)
            {
                nextBuffSample=now+.1;
                buffs.Observe(player!.EntityId,now,player.StatusList.Take(64).Select(s=>new BuffReading(s.StatusId,s.SourceId,s.RemainingTime,s.Param)),
                    source=>source==0 || source==0xE0000000?"未知 / unknown":Objects.SearchById(source)?.Name.ToString() ?? $"未观测来源 / unseen #{source:X}");
            }
            for(var processed=0;processed<512 && queue.Reader.TryRead(out var observation);processed++)
            {
                if (observation.Kind == 3) { model.SharedCooldown(observation.Time, observation.Lock, observation.Duration, observation.Action); continue; }
                var info = Info(observation.Action); if (info == null) continue;
                switch (observation.Kind)
                {
                    case 0: model.Requested(info, observation.Time, observation.Target, observation.Queued); break;
                    case 1:
                        var entry = model.Acknowledged(info, observation.Time, observation.Target, observation.Sequence, observation.Global, observation.Lock);
                        if (manager != null)
                        {
                            var ownGroup = info.PrimaryGroup == 58 ? info.AdditionalGroup : info.PrimaryGroup;
                            if (ownGroup is > 0 and <= 80)
                            {
                                var cd = manager->GetRecastGroupDetail(ownGroup - 1);
                                if (cd != null && cd->IsActive && float.IsFinite(cd->Total))
                                {
                                    if(TimelinePresentation.ShortRecastAction(info.Id) && (cd->ActionId==info.Id || cd->ActionId==0)) model.OwnCooldown(info.Id,now,cd->Elapsed,cd->Total);
                                    else if(cd->ActionId==info.Id) { entry.CooldownDuration=cd->Total;entry.CooldownStart=now-cd->Elapsed; }
                                }
                            }
                        }
                        break;
                    case 2: model.Effect(info, observation.Time, (uint)observation.Target, observation.EffectType, observation.Value, observation.Sequence, observation.Global, observation.DamageFlags); break;
                    case 4: model.EmptyPayload(info.Id,observation.Time);break;
                    case 5: MountObservation.ReceiveMount(model,info,observation.Time,observation.Target,observation.Sequence,observation.Global,observation.Lock);break;
                }
            }
            if (player!.IsCasting && MountObservation.CastAction(player.CastActionType,player.CastActionId) is var castId && castId!=0 && Info(castId) is { } castInfo)
            {
                if (casting != null && casting.Info.Id != castId) casting.CastEnd = now;
                casting = model.Casting(castInfo, now, player.CurrentCastTime, player.TotalCastTime, player.TargetObjectId);
            }
            else if (casting != null) { casting.CastEnd = now; casting = null; }
            if (manager != null)
            {
                model.SampleLock(now, manager->AnimationLock);
                var gcd = manager->GetRecastGroupDetail(57);
                if (gcd != null && gcd->IsActive) model.SharedCooldown(now, gcd->Elapsed, gcd->Total, gcd->ActionId);
                // 自愈独立的一秒复唱，与共享组58完全分开采集；其他长冷却能力技不加蓝条。
                foreach(var recuperate in model.Entries.Where(e=>TimelinePresentation.ShortRecastAction(e.Info.Id) && !e.TriggeredEffect && now-(e.Ack ?? e.Start)<2).ToArray())
                {
                    if(recuperate.Info.PrimaryGroup is not (>0 and <=80))continue;
                    var own=manager->GetRecastGroupDetail(recuperate.Info.PrimaryGroup-1);
                    if(own!=null && own->IsActive && (own->ActionId==recuperate.Info.Id || own->ActionId==0)) model.OwnCooldown(recuperate.Info.Id,now,own->Elapsed,own->Total);
                }
            }
            if(!config.ObserveNativeAnimations)animationObserver.Reset("native_observation_disabled");
            animationObserver.Sample();
            for(var processed=0;processed<512 && animationQueue.Reader.TryRead(out var message);processed++)
            {
                if (message.Request != null) model.NativeRequest(message.Request);
                if (message.Signal != null) model.NativeAnimation(message.Signal);
            }
            model.Prune(now);
        }
        catch (Exception e)
        {
            Volatile.Write(ref capturing, false);
            if (!observationFault) Log.Warning(e, "PvP Timeline paused after observation failure; retry on map change");
            observationFault = true;
        }
    }
    private void Draw()
    {
        UiText.Language=config.Language;
        if (capturing && (!config.CombatOnly || Condition[ConditionFlag.InCombat]) &&
            (!config.HideInCutscene || (!Condition[ConditionFlag.WatchingCutscene] && !Condition[ConditionFlag.OccupiedInCutSceneEvent])))
        {
            window.Draw(frozen ? frozenTime : Now, dropped, frozen,buffs,GetBuffInfo,Objects.LocalPlayer?.ClassJob.RowId??0);
        }
        if (!settingsVisible) return;
        ImGui.SetNextWindowSize(new Vector2(650, 510), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("PvP Timeline — "+UiText.T("Settings")+"###pvp-timeline-settings", ref settingsVisible))
        {
            ImGui.TextUnformatted("PvP Timeline");ImGui.SameLine();
            if (ImGui.Button(UiText.T(frozen ? "Live" : "Freeze"))) { frozen = !frozen; frozenTime = Now; }
            ImGui.SameLine(); if (ImGui.Button(UiText.T("Clear"))) {model.Clear();buffs.Clear();}
            var previousLanguage=config.Language;var changed = window.DrawSettings();
            if (changed)
            {
                config.Normalize();Pi.SavePluginConfig(config);
                if(previousLanguage!=config.Language){actions.Clear();buffMetadata.Clear();foreach(var e in model.Entries)if(Info(e.Info.Id) is {} translated)e.Info=e.Info with {Name=translated.Name};}
            }
            if (ImGui.CollapsingHeader(UiText.T("Diagnostics")))
            {
                ImGui.TextWrapped(hookState);
                ImGui.Text($"territory={territory}; duty={duty}; observing={capturing}; fault={observationFault}");
                ImGui.Text(UiText.T("Cached")+$": {model.Entries.Count}/256 · "+UiText.T("Dropped events")+$": {dropped} · "+UiText.T("Ambiguous")+$": {model.AmbiguousEffects}");
                ImGui.Text(UiText.T("Unmatched animations")+$": {model.UnmatchedAnimationCount}");
                ImGui.Text(UiText.T("Damage glow")+": "+UiText.T(window.GlowState));
                if(ImGui.Button(UiText.T("Copy effect diagnostics")))
                {
                    var report=System.Text.Json.Nodes.JsonNode.Parse(model.EffectDiagnostics())!;
                    report["pluginVersion"]=typeof(Plugin).Assembly.GetName().Version?.ToString();
                    report["ownStatuses"]=System.Text.Json.JsonSerializer.SerializeToNode(Objects.LocalPlayer?.StatusList.Take(64).Select(s=>new{id=s.StatusId,source=s.SourceId,remaining=s.RemainingTime,name=GetBuffInfo(s.StatusId)?.Name,allowed=BuffLayout.FilterPositive(s.StatusId,Objects.LocalPlayer?.ClassJob.RowId??0)}).ToArray());
                    ImGui.SetClipboardText(report.ToJsonString());
                }
                ImGui.TextWrapped(System.Text.Json.JsonSerializer.Serialize(animationObserver.Diagnostics));
            }
        }
        ImGui.End();
    }
    public void Dispose()
    {
        Volatile.Write(ref capturing, false);
        Framework.Update -= Update; Pi.UiBuilder.Draw -= Draw; Pi.UiBuilder.OpenConfigUi -= OpenSettings;
        ClientState.TerritoryChanged -= OnAreaChanged;
        ClientState.Login -= OnLogin;
        Commands.RemoveHandler("/pvptimeline"); requestHook?.Dispose(); effectHook?.Dispose(); animationObserver.Reset("plugin_unload"); animationObserver.Dispose();
        queue.Writer.TryComplete(); animationQueue.Writer.TryComplete(); model.Clear();
        window.Dispose();
        UiText.Release();
    }
}
