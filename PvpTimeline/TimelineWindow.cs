using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;

namespace PvpTimeline;

/// <summary>固定刻度、单图标、两排轮换能力技；动画合并为独立轨道，不叠画条和不透明标记。</summary>
internal sealed class TimelineWindow(Configuration config, TimelineModel model, ITextureProvider textures, Action openSettings)
{
    private Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap? damageGlow;
    public string GlowState {get;private set;}="not_initialized";
    public void InitializeGlow(IDataManager data)
    {
        // 旧版XIVConfigUI DrawSlotHighlight：白色RGB+原alpha，取480..624柔光区域。
        try
        {
            var asset=data.GetFile<Lumina.Data.Files.TexFile>("ui/uld/icona_frame_hr1.tex");
            if(asset==null){GlowState="asset_missing";return;}
            var pixels=asset.ImageData;
            if(pixels.Length>16*1024*1024 || pixels.Length%4!=0){GlowState="invalid_asset";return;}
            var white=new byte[pixels.Length];
            for(var i=0;i<pixels.Length;i+=4){white[i]=white[i+1]=white[i+2]=255;white[i+3]=pixels[i+3];}
            damageGlow=textures.CreateFromRaw(RawImageSpecification.Rgba32(asset.Header.Width,asset.Header.Height),white);
            GlowState="ready";
        }
        catch(Exception ex){GlowState="failed:"+ex.GetType().Name;}
    }
    public void Dispose()=>damageGlow?.Dispose();
    private BuffSpan[]? frozenBuffs;
    private readonly List<(Vector2 Min,Vector2 Max)> endpointUnderlines=new(256);
    public Vector2 BuffAnchor {get;private set;}
    public float OverlayWidth {get;private set;}=1000;
    public float OverlayHeight {get;private set;}=170;
    private static uint Color(Vector4 color, float opacity = 1) => ImGui.ColorConvertFloat4ToU32(color with { W = color.W * opacity });
    private static double Anchor(TimelineEntry e) => e.GcdStart ?? (TimelinePresentation.ShortRecastAction(e.Info.Id) ? e.CooldownStart : null) ?? e.ExecutionStart ?? e.Ack ?? e.Start;
    public void Draw(double now, long dropped, bool frozen,BuffState buffs,Func<uint,BuffInfo?> buffInfo,uint job=0)
    {
        if(!frozen)frozenBuffs=null;
        else frozenBuffs??=buffs.Snapshot();
        var flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar;
        if (config.Locked) flags |= ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMouseInputs;
        ImGui.SetNextWindowSize(new Vector2(1000, 170), ImGuiCond.FirstUseEver);
        // The overlay is user-resizable; content size must not force a fixed window height.
        ImGui.SetNextWindowSizeConstraints(new Vector2(120,64),new Vector2(float.MaxValue));
        ImGui.SetNextWindowPos(new Vector2(180, 180), ImGuiCond.FirstUseEver);
        ImGui.PushStyleColor(ImGuiCol.WindowBg,(config.Locked?config.LockedBackground:config.UnlockedBackground) with {W=config.Locked?config.LockedOpacity:config.UnlockedOpacity});
        if (ImGui.Begin("PvP Timeline##overlay", flags))
        {
            ImGui.SetWindowFontScale(config.UiScale);
            BuffAnchor=ImGui.GetWindowPos()+new Vector2(0,ImGui.GetWindowSize().Y);
            OverlayWidth=ImGui.GetWindowSize().X;
            OverlayHeight=ImGui.GetWindowSize().Y;
            var origin = ImGui.GetCursorScreenPos();
            var size = ImGui.GetContentRegionAvail();
            var draw = ImGui.GetWindowDrawList();
            var extent = config.Horizontal ? size.X : size.Y;
            var cross = config.Horizontal ? size.Y : size.X;
            var view = new TimelineViewport(extent, (config.PixelsPerSecond*config.UiScale), config.FutureSeconds, config.HistorySeconds, config.Reverse);
            var first = now + view.FirstOffset;
            var last = now + config.FutureSeconds;
            Vector2 Offset(double offset, float row) => origin + (config.Horizontal
                ? new Vector2(view.OffsetPosition(offset), row) : new Vector2(row, view.OffsetPosition(offset)));
            Vector2 Point(double time, float row) => Offset(time - now, row);
            void Line(double from, double to, float row, Vector4 color, float? width = null)
            {
                if (!double.IsFinite(from) || !double.IsFinite(to) || to < from || to < first || from > last) return;
                var p = Point(Math.Max(from, first), row);
                var q = Point(Math.Min(to, last), row);
                var half = (width ?? (config.LineThickness*config.UiScale)) / 2;
                var pad = config.Horizontal ? new Vector2(0, half) : new Vector2(half, 0);
                var min = Vector2.Min(p,q)-pad; var max = Vector2.Max(p,q)+pad;
                draw.AddRectFilled(min, max, Color(color, config.BarFillOpacity));
                draw.AddRect(min, max, Color(color), 0, ImDrawFlags.None, 1);
            }
            void Diamond(double time, float row, Vector4 color,bool direct)
            {
                if (time < first || time > last) return;
                var p = Point(time, row); var r = (config.EffectMarkerSize*config.UiScale);
                if(direct) draw.AddQuadFilled(p + new Vector2(0,-r), p + new Vector2(r,0), p + new Vector2(0,r), p + new Vector2(-r,0), Color(color));
                else draw.AddQuad(p + new Vector2(0,-r), p + new Vector2(r,0), p + new Vector2(0,r), p + new Vector2(-r,0), Color(color),1);
            }
            var gcdRow = 24*config.UiScale + Math.Max(26*config.UiScale, -(config.AbilityVerticalOffset*config.UiScale)) + (config.AbilityIconSize*config.UiScale) + (config.AbilityLaneGap*config.UiScale);
            var abilityRow = gcdRow + (config.AbilityVerticalOffset*config.UiScale);
            float Row(TimelineEntry e) => e.Info.Ability ? abilityRow - e.AbilityLane * ((config.AbilityIconSize*config.UiScale) + (config.AbilityLaneGap*config.UiScale)) : gcdRow;
            bool Visible(TimelineEntry e) => TimelinePresentation.ShowEntry(e) && Anchor(e) <= now &&
                (e.Info.Ability ? config.ShowAbilities : e.Info.Category != 1 || config.ShowAutoAttack) &&
                (e.Ack != null || e.CastObserved || e.GcdDuration != null || e.Effects.Count>0 || config.ShowRecuperateCooldown && TimelinePresentation.RecuperateRecast(e));
            draw.PushClipRect(origin, origin + size, true);
            // 固定相对秒数与像素位置，时间流逝只影响Point投影的事件，不移动网格。
            foreach (var offset in view.Ticks(config.Subdivisions))
            {
                var whole = Math.Abs(offset - Math.Round(offset)) < .001;
                if (config.ShowGrid) draw.AddLine(Offset(offset, 18), Offset(offset, cross), whole ? 0x40505050u : 0x20505050u);
                if (whole && config.ShowSeconds) draw.AddText(Offset(offset, 0), 0xFFCCCCCC, $"{offset:+0;-0;0}s");
            }
            draw.AddLine(Offset(0, 16), Offset(0, cross), 0xFFEFEFEF, 2);
            if (config.ShowAnimation)
            {
                var animationRow = gcdRow + (config.GcdIconSize*config.UiScale) + (config.AnimationVerticalOffset*config.UiScale);
                var spans = model.Entries.SelectMany(e => e.Animations).Distinct().ToArray();
                foreach (var band in TimelinePresentation.AnimationBands(spans, now))
                    Line(band.Start, band.End, animationRow, config.AnimationColor);
                // 只有明确被另一技能覆盖才画中断标志；普通结束、槽位清空、循环和未知原因不画。
                foreach (var animation in spans.Where(a => a.End is { } end && end >= first && end <= now &&
                             TimelinePresentation.ShowInterruption(a))
                             .DistinctBy(a => Math.Round(a.End!.Value, 3)))
                {
                    var p = Point(animation.End!.Value, animationRow);
                    draw.AddQuad(p+new Vector2(0,-4), p+new Vector2(4,0), p+new Vector2(0,4), p+new Vector2(-4,0), Color(config.AnimationReplacedColor), 1);
                }
            }
            var effectMarks = new List<(double Time, float Row,bool Direct)>();
            endpointUnderlines.Clear();
            // 先画全部条，后画全部图标，旧条不会压到下一技能的图标上。
            foreach (var e in model.Entries.Where(Visible))
            {
                var anchor = Anchor(e);
                var iconSize = e.Info.Ability ? (config.AbilityIconSize*config.UiScale) : (config.GcdIconSize*config.UiScale);
                var row = Row(e);
                var bar = TimelinePresentation.BarY(e.Info.Ability,row,iconSize,config.GcdBarOffset,config.AbilityBarOffset,config.UiScale);
                var thickness=TimelinePresentation.BarThickness(e.Info.Ability,config.LineThickness,config.AbilityLineThickness,config.UiScale);
                // 条不能跨到此技能时间锚点之前（含边框）；横/竖、正/反方向使用同一裁剪规则。
                var boundary=Point(anchor,0);
                var clipMin=origin;var clipMax=origin+size;
                if(config.Horizontal) { if(config.Reverse)clipMax.X=Math.Min(clipMax.X,boundary.X);else clipMin.X=Math.Max(clipMin.X,boundary.X); }
                else { if(config.Reverse)clipMax.Y=Math.Min(clipMax.Y,boundary.Y);else clipMin.Y=Math.Max(clipMin.Y,boundary.Y); }
                draw.PushClipRect(clipMin,Vector2.Max(clipMin,clipMax),true);
                if (config.ShowGcd && e.GcdStart is { } start && e.GcdDuration is { } duration)
                    Line(start, start + duration, bar, config.GcdColor,thickness);
                var shortRecast=config.ShowRecuperateCooldown && TimelinePresentation.RecuperateRecast(e);
                if(shortRecast) Line(e.CooldownStart!.Value,e.CooldownStart.Value+e.CooldownDuration!.Value,bar,config.GcdColor,thickness);
                if (config.ShowCast && e.CastObserved)
                    Line(e.ExecutionStart ?? e.Start, e.CastEnd ?? Math.Min(now, (e.ExecutionStart ?? e.Start) + e.CastDuration),
                        bar + (config.CastVerticalOffset*config.UiScale), config.CastColor,thickness);
                if (config.ShowLock && e.LockStart is { } lockStart)
                {
                    var stripe=TimelinePresentation.LockStripe(thickness);
                    Line(lockStart, e.LockEnd ?? lockStart + e.ReportedLock, bar+stripe.Offset, config.LockColor,stripe.Thickness);
                    var lockEnd=e.LockEnd ?? lockStart+e.ReportedLock;
                    var a=Point(lockStart,bar-5);var b=Point(lockEnd,bar+6);
                    if(!config.Locked && ImGui.IsMouseHoveringRect(Vector2.Min(a,b),Vector2.Max(a,b)))
                    {
                        ImGui.BeginTooltip();ImGui.TextUnformatted(e.Info.Name);
                        ImGui.Text(UiText.T("Lock")+": "+Seconds(lockEnd-lockStart));ImGui.EndTooltip();
                    }
                }
                if (config.ShowCooldown && !shortRecast && e.CooldownDuration is { } cooldown && (e.CooldownStart ?? e.Ack) is { } cdStart)
                    Line(cdStart, cdStart + cooldown, bar + 10, config.CooldownColor, 2);
                draw.PopClipRect();
                // 同一技能只放一个目标效果标记：优先伤害，其次治疗，最后状态；多目标不堆叠菱形。
                if (config.ShowEffects)
                {
                    var effect = TimelinePresentation.PrimaryEffect(e,now);
                    if (effect != null) effectMarks.Add((effect.Time, bar,TimelinePresentation.DirectImpact(effect.Type) || e.Info.IsMount && effect.Type==39 || e.EmptyReturnConfirmation==effect.Time && TimelinePresentation.MovementReceipt(e.Info.Id)));
                }
            }
            foreach (var e in model.Entries.Where(Visible))
            {
                var anchor = Anchor(e);
                var iconSize = e.Info.Ability ? (config.AbilityIconSize*config.UiScale) : (config.GcdIconSize*config.UiScale);
                var row = Row(e);
                // 所有朝向统一以条起点放图标；不把图标向后推，避免左端露条。
                var p0 = Point(TimelinePresentation.IconTime(anchor), row);
                if (config.Reverse) p0 -= config.Horizontal ? new Vector2(iconSize, 0) : new Vector2(0, iconSize);
                var p1 = p0 + new Vector2(iconSize);
                if (p1.X < origin.X || p1.Y < origin.Y || p0.X > origin.X+size.X || p0.Y > origin.Y+size.Y) continue;
                var opacity = e.TriggeredEffect ? config.TriggeredIconOpacity : e.Ack == null ? .65f : 1;
                var wrap = textures.GetFromGameIcon(new GameIconLookup(e.Info.Icon, hiRes: true)).GetWrapOrDefault();
                if (wrap != null) draw.AddImage(wrap.Handle, p0, p1, Vector2.Zero, Vector2.One, Color(Vector4.One, opacity));
                else draw.AddText(p0, 0xFFFFFFFF, e.Info.Id.ToString());
                var damageFlags = e.DamageFlagsAt(now);
                if (config.HighlightDamage && (damageFlags & 0x60) != 0)
                {
                    var color = (damageFlags & 0x60) == 0x60 ? config.CriticalDirectColor : (damageFlags & 0x20) != 0 ? config.CriticalColor : config.DirectColor;
                    // 游戏槽位高亮图层，取代手画的粗矩形框。
                    var frame=damageGlow;
                    if(frame!=null)
                    {
                        var scale=iconSize/82f;var corner=p0-new Vector2(31*scale);
                        var uv=new Vector2(480f/852f,0);
                        draw.AddImage(frame.Handle,corner,corner+new Vector2(144*scale,154*scale),uv,new Vector2(624f/852f,.5f),Color(color));
                    }
                }
                if (!config.Locked && ImGui.IsMouseHoveringRect(p0, p1)) Tooltip(e, now);
            }
            foreach (var mark in effectMarks) Diamond(mark.Time, mark.Row, config.EffectColor,mark.Direct);
            if(config.ShowOwnBuffs)
            {
                var buffIconSize=16*config.UiScale;
                var baseRow=gcdRow+(config.GcdIconSize*config.UiScale)+(config.AnimationVerticalOffset*config.UiScale)+8;
                var lanes=new double[config.BuffRows];var overflow=0;
                var history=frozenBuffs??buffs.History.ToArray();
                var benefits=history.Where(s=>s.Start<=now && (s.End??s.Through)>=first && BuffLayout.IsBenefit(s.Id,buffInfo(s.Id)) && BuffLayout.FilterPositive(s.Id,job)).Take(256).ToArray();
                // 计时队列：左端图标按剩余时间排列，同一像素/秒比例下右端图标逐渐靠近。
                // 所有实际实例都参与，不因同名而合并不同来源；不以预测归零删除实际仍存在的状态。
                var slots=Math.Max(1,(int)(extent/(18*config.UiScale)));
                var placements=BuffLayout.Place(benefits,now,first,(config.PixelsPerSecond*config.UiScale),buffIconSize).Take(slots).ToArray();
                foreach(var item in placements)Line(item.Left,Math.Min(item.End,last),baseRow+8,config.CastColor,4);
                foreach(var item in placements)
                {
                    var span=item.Span;var info=buffInfo(span.Id);var remaining=span.RemainingAt(now);
                    var left=item.Left;var end=item.End;
                    var wrap=info is {Icon:>0}?textures.GetFromGameIcon(new GameIconLookup(info.Icon)).GetWrapOrDefault():null;
                    var p=Point(left,baseRow);if(config.Reverse)p-=config.Horizontal?new Vector2(buffIconSize,0):new Vector2(0,buffIconSize);
                    if(wrap!=null)
                    {
                        draw.AddImage(wrap.Handle,p,p+new Vector2(buffIconSize));
                        if(end<=last)
                        {
                            var q=Point(end,baseRow);if(config.Reverse)q-=config.Horizontal?new Vector2(buffIconSize,0):new Vector2(0,buffIconSize);
                            draw.AddImage(wrap.Handle,q,q+new Vector2(buffIconSize));
                            if(BuffLayout.MarkEndpoint(span,now))
                                endpointUnderlines.Add((q+new Vector2(0,18*config.UiScale),q+new Vector2(buffIconSize,20*config.UiScale)));
                        }
                    }
                    if(item.Counter.Length>0)
                    {
                        var textWidth=ImGui.CalcTextSize(item.Counter).X*9*config.UiScale/ImGui.GetFontSize();
                        draw.AddText(ImGui.GetFont(),9*config.UiScale,p+new Vector2(Math.Max(1,(buffIconSize-textWidth)/2),15*config.UiScale),0xFFFFFFFF,item.Counter);
                    }
                    if(!config.Locked && ImGui.IsMouseHoveringRect(p,p+new Vector2(buffIconSize)))
                    {ImGui.BeginTooltip();ImGui.Text($"{info?.Name??span.Id.ToString()} · {UiText.SourceName(span.SourceName)}");ImGui.Text(remaining is {} seconds?UiText.T("Remaining")+$": {seconds:F1} s":UiText.T("No personal countdown"));ImGui.EndTooltip();}
                }
                if(benefits.Length>slots)draw.AddText(Point(now,baseRow),0xFFCCCCCC,$"+{benefits.Length-slots}");
                foreach(var span in history.Where(s=>s.Start<=now && (s.End??s.Through)>=first && !BuffLayout.IsBenefit(s.Id,buffInfo(s.Id))).OrderBy(s=>s.Start))
                {
                    var info=buffInfo(span.Id);var group=info?.Harmful==true?1:0;
                    var lane=-1;for(var i=0;i<config.BuffRows;i++)if((config.BuffRows==1 || i==group) && lanes[i]<=span.Start){lane=i;break;}
                    if(lane<0){overflow++;continue;}
                    var end=Math.Min(span.End??span.Through,now);lanes[lane]=end+.1;
                    var row=baseRow+lane*(config.BuffRowHeight*config.UiScale);var color=group==0?config.CastColor:config.EffectColor;
                    Line(span.Start,end,row+8,color,5);
                    foreach(var change in span.Changes.Where(c=>c.Time<=now && c.Time>=first && (c.Refresh || c.Parameter>0)))
                    {
                        var at=Point(change.Time,row+8);draw.AddLine(at-new Vector2(0,5),at+new Vector2(0,5),Color(color),1);
                    }
                    var p=Point(Math.Max(span.Start,first),row);if(config.Reverse)p-=config.Horizontal?new Vector2(buffIconSize,0):new Vector2(0,buffIconSize);
                    if(info is {Icon:>0} && textures.GetFromGameIcon(new GameIconLookup(info.Icon)).GetWrapOrDefault() is {} icon)
                        draw.AddImage(icon.Handle,p,p+new Vector2(buffIconSize));
                    var changeAt=span.At(now);
                    if((span.End==null || span.End>now) && span.Through>=now-1)
                    {
                        // 仅本人实测倒数投影未来，以断线区别已观测区间。
                        if(changeAt is {Remaining:>0} && float.IsFinite(changeAt.Remaining))
                        {
                            var predicted=changeAt.Time+changeAt.Remaining;
                            for(var t=now;t<Math.Min(predicted,last);t+=.2)Line(t,Math.Min(t+.08,predicted),row+8,color,1);
                        }
                    }
                    var q=Point(end,row+buffIconSize);var lo=Vector2.Min(p,q);var hi=Vector2.Max(p+new Vector2(buffIconSize),q);
                    if(!config.Locked && ImGui.IsMouseHoveringRect(lo,hi))
                    {
                        ImGui.BeginTooltip();ImGui.Text($"{info?.Name??span.Id.ToString()} · {UiText.SourceName(span.SourceName)}");
                        ImGui.Text(UiText.T("Observed duration")+$": {Math.Max(0,end-span.Start):F2} s · "+UiText.T(span.End<=now?span.Gap?"Observation gap":"Removed":"Present"));
                        ImGui.Text(UiText.T("Parameter")+$": {changeAt?.Parameter} · "+UiText.T("Refreshes")+$": {span.Changes.Count(c=>c.Refresh && c.Time<=now)}");
                        ImGui.EndTooltip();
                    }
                }
                if(overflow>0)
                {
                    var p=Point(now,baseRow);draw.AddText(p,0xFFCCCCCC,$"+{overflow}");
                    if(!config.Locked && ImGui.IsMouseHoveringRect(p,p+new Vector2(50,18)))
                    {ImGui.BeginTooltip();foreach(var s in (frozenBuffs??buffs.History.ToArray()).Where(s=>s.Start<=now && (s.End??s.Through)>=first).Take(64))ImGui.Text($"{buffInfo(s.Id)?.Name??s.Id.ToString()} · {UiText.SourceName(s.SourceName)}");ImGui.EndTooltip();}
                }
            }
            // Draw endpoint identifiers last so counters/debuff icons cannot cover the red strip.
            foreach(var marker in endpointUnderlines)draw.AddRectFilled(marker.Min,marker.Max,0xFF3030FF);
            if (dropped > 0 || frozen) draw.AddText(origin, 0xFF66CCFF, frozen ? UiText.T("Frozen") : UiText.T("Dropped events")+$": {dropped}");
            draw.PopClipRect();
            if (ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right)) openSettings();
        }
        ImGui.End();ImGui.PopStyleColor();
    }
    private static string Seconds(double? value) => value is { } number ? $"{number:F3} s" : UiText.T("Unknown");
    private static void Tooltip(TimelineEntry e,double now)
    {
        ImGui.BeginTooltip();
        ImGui.TextUnformatted(e.Info.Name);
        ImGui.Text(UiText.T("GCD")+": "+Seconds(e.GcdDuration));
        if(e.CastObserved)ImGui.Text(UiText.T("Cast")+": "+Seconds(e.CastDuration));
        if(e.CooldownDuration!=null)ImGui.Text(UiText.T("Cooldown")+": "+Seconds(e.CooldownDuration));
        ImGui.EndTooltip();
    }
    public bool DrawSettings()
    {
        var changed = false;
        UiText.Language=config.Language;
        changed |= ImGui.Combo(UiText.T("Language"),ref config.Language,"简体中文\0繁體中文\0English\0Deutsch\0Français\0日本語\0");
        UiText.Language=config.Language;
        changed |= Number("Scale",ref config.UiScale,.2f,2);
        if (!ImGui.BeginTabBar("timeline-settings")) return false;
        if (ImGui.BeginTabItem(UiText.T("显示 / Display")))
        {
            changed |= ImGui.Checkbox(UiText.T("启用 / Enabled"), ref config.Enabled);
            changed |= ImGui.Checkbox(UiText.T("锁定（鼠标穿透） / Locked"), ref config.Locked);
            changed |= ImGui.Checkbox(UiText.T("仅战斗中 / Combat only"), ref config.CombatOnly);
            changed |= ImGui.Checkbox(UiText.T("过场隐藏 / Hide in cutscene"), ref config.HideInCutscene);
            changed |= ImGui.Checkbox(UiText.T("自身Buff历史轴 / Own status history"),ref config.ShowOwnBuffs);
            changed |= ImGui.Checkbox(UiText.T("原生动画采集（涉及游戏指针） / Native animation observation"),ref config.ObserveNativeAnimations);
            changed |= NumberInt("状态轴行数 / Status rows",ref config.BuffRows,1,2);
            changed |= Number("状态轴行高 / Status row height",ref config.BuffRowHeight,14,28);
            changed |= ImGui.Checkbox(UiText.T("横向 / Horizontal"), ref config.Horizontal);
            changed |= ImGui.Checkbox(UiText.T("反向 / Reverse"), ref config.Reverse);
            changed |= Background("Locked background",ref config.LockedBackground,ref config.LockedOpacity);
            changed |= Background("Unlocked background",ref config.UnlockedBackground,ref config.UnlockedOpacity);
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem(UiText.T("排版 / Layout")))
        {
            changed |= Number("GCD图标 / GCD size", ref config.GcdIconSize, 24, 80);
            changed |= Number("能力技图标 / Ability size", ref config.AbilityIconSize, 16, 60);
            changed |= Number("能力技高度 / Ability offset", ref config.AbilityVerticalOffset, -100, 40);
            changed |= Number("插入技排间距 / Lane gap", ref config.AbilityLaneGap, 0, 24);
            changed |= Number("条内填充 / Fill opacity", ref config.BarFillOpacity, .05f, .8f);
            changed |= Number("公CD条位置 / Bar offset", ref config.GcdBarOffset, -40, 60);
            changed |= Number("能力技条位置 / Ability bar offset",ref config.AbilityBarOffset,-40,60);
            changed |= Number("读条位置 / Cast offset", ref config.CastVerticalOffset, -40, 40);
            changed |= Number("动画条位置 / Animation offset", ref config.AnimationVerticalOffset, 2, 80);
            changed |= Number("GCD条宽 / GCD bar thickness", ref config.LineThickness, 1, 12);
            changed |= Number("能力技条宽 / Ability bar thickness",ref config.AbilityLineThickness,1,12);
            changed |= Number("菱形大小 / Marker size", ref config.EffectMarkerSize, 2, 10);
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem(UiText.T("轨道 / Tracks")))
        {
            changed |= Track("公CD / GCD", ref config.ShowGcd, ref config.GcdColor);
            changed |= Track("读条 / Cast", ref config.ShowCast, ref config.CastColor);
            changed |= Track("动作锁 / Lock", ref config.ShowLock, ref config.LockColor);
            changed |= Track("动画 / Animation", ref config.ShowAnimation, ref config.AnimationColor);
            changed |= Track("目标效果 / Effects", ref config.ShowEffects, ref config.EffectColor);
            changed |= Track("独立冷却 / Cooldown", ref config.ShowCooldown, ref config.CooldownColor);
            changed |= ImGui.Checkbox(UiText.T("自愈／暗影锋自身短冷却 / Short own recast"),ref config.ShowRecuperateCooldown);
            changed |= ImGui.Checkbox(UiText.T("能力技 / Abilities"), ref config.ShowAbilities);
            changed |= ImGui.Checkbox(UiText.T("自动攻击 / Auto attacks"), ref config.ShowAutoAttack);
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem(UiText.T("时间 / Time")))
        {
            changed |= Number("每秒像素 / px per second", ref config.PixelsPerSecond, 20, 150);
            changed |= Number("历史秒数 / History", ref config.HistorySeconds, 5, 60);
            changed |= Number("预留秒数 / Future", ref config.FutureSeconds, 0, 10);
            changed |= ImGui.Checkbox(UiText.T("固定竖网格 / Fixed grid"), ref config.ShowGrid);
            changed |= ImGui.Checkbox(UiText.T("固定秒数 / Seconds"), ref config.ShowSeconds);
            changed |= NumberInt("每秒细分 / Subdivisions", ref config.Subdivisions, 1, 8);
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem(UiText.T("高亮 / Feedback")))
        {
            changed |= ImGui.Checkbox(UiText.T("暴击/直击槽位高亮 / Damage highlight"), ref config.HighlightDamage);
            changed |= ImGui.ColorEdit4(UiText.T("暴击 / Critical"), ref config.CriticalColor, ImGuiColorEditFlags.AlphaBar);
            changed |= ImGui.ColorEdit4(UiText.T("直击 / Direct"), ref config.DirectColor, ImGuiColorEditFlags.NoInputs);
            changed |= ImGui.ColorEdit4(UiText.T("直暴 / Critical direct"), ref config.CriticalDirectColor, ImGuiColorEditFlags.NoInputs);
            changed |= ImGui.ColorEdit4(UiText.T("动画被覆盖 / Animation replaced"), ref config.AnimationReplacedColor, ImGuiColorEditFlags.NoInputs);
            changed |= Number("被动效果虚影 / Trigger opacity",ref config.TriggeredIconOpacity,.15f,.75f);
            ImGui.EndTabItem();
        }
        ImGui.EndTabBar(); return changed;
    }
    private readonly Dictionary<string,string> htmlColors=new();
    // Numeric editors share units and wheel semantics; model values remain in canonical units.
    private static float WheelBeforeEditor()
    {
        var cursor=ImGui.GetCursorScreenPos();
        var hovered=ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem) &&
            ImGui.IsMouseHoveringRect(cursor,cursor+new Vector2(ImGui.CalcItemWidth(),ImGui.GetFrameHeight()));
        var wheel=hovered?ImGui.GetIO().MouseWheel:0;
        if(wheel!=0 && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            // Clear the old text edit buffer before InputFloat/Int formats the changed value.
            // This operates on ImGui state only, never on game input or an action manager.
            ImGuiP.ClearActiveID();return wheel;
        }
        return 0;
    }
    private static void ClaimEditorWheel()
    {
        // Register every hovered frame so ImGui will not route the next wheel event to the page.
        if(ImGui.IsItemHovered())ImGuiP.SetItemUsingMouseWheel();
    }
    private static bool Number(string label,ref float value,float min,float max)
    {
        var key=label.Split(" / ")[^1].Split("##")[0];
        var percent=key is "Fill opacity" or "Trigger opacity" or "Opacity";
        var scale=percent?100f:1f;var shown=value*scale;
        var unit=percent?"%%":key=="Scale"?"×":key is "History" or "Future"?"s":key=="px per second"?"px/s":UiText.T("Pixels");
        var wheel=WheelBeforeEditor();var changed=wheel!=0;
        if(changed)shown=NumericWheel.Apply(shown,wheel,key=="Scale"?.05f:1,min*scale,max*scale);
        changed|=ImGui.InputFloat(UiText.T(label),ref shown,0,0,(percent?"%.0f ":"%.2f ")+unit);
        ClaimEditorWheel();
        if(changed)value=float.IsFinite(shown)?Math.Clamp(shown/scale,min,max):min;
        return changed;
    }
    private static bool NumberInt(string label,ref int value,int min,int max)
    {
        var unit=label.Contains("Subdivisions")?UiText.T("per second"):UiText.T("Rows");
        var wheel=WheelBeforeEditor();var changed=wheel!=0;
        if(changed)value=(int)MathF.Round(NumericWheel.Apply(value,wheel,1,min,max));
        changed|=ImGui.InputInt(UiText.T(label)+" ("+unit+")",ref value,0,0);
        ClaimEditorWheel();
        if(changed)value=Math.Clamp(value,min,max);return changed;
    }
    private bool Background(string label,ref Vector4 color,ref float alpha)
    {
        color.W=alpha;var changed=ImGui.ColorEdit4(UiText.T(label),ref color,ImGuiColorEditFlags.NoInputs);
        if(changed)htmlColors.Remove(label);
        var text=htmlColors.TryGetValue(label,out var saved)?saved:$"#{(int)(color.X*255):X2}{(int)(color.Y*255):X2}{(int)(color.Z*255):X2}{(int)(color.W*255):X2}";
        if(ImGui.InputText("HTML##"+label,ref text,10))
        {
            htmlColors[label]=text;var hex=text.TrimStart('#');
            if((hex.Length==6 || hex.Length==8) && uint.TryParse(hex,System.Globalization.NumberStyles.HexNumber,System.Globalization.CultureInfo.InvariantCulture,out var rgba))
            {if(hex.Length==6)rgba=(rgba<<8)|(uint)(color.W*255);color=new((rgba>>24&255)/255f,(rgba>>16&255)/255f,(rgba>>8&255)/255f,(rgba&255)/255f);changed=true;}
        }
        alpha=color.W;changed|=Number("Opacity##"+label,ref alpha,0,1);color.W=alpha;return changed;
    }
    private static bool Track(string label, ref bool enabled, ref Vector4 color)
    {
        var changed = ImGui.Checkbox(UiText.T(label), ref enabled); ImGui.SameLine();
        changed |= ImGui.ColorEdit4(UiText.T("##color-")+label, ref color, ImGuiColorEditFlags.NoInputs); return changed;
    }
}
