using System.Diagnostics;
using System.Threading.Channels;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace PvpSkillLab;

/// <summary>两个插件分别编译此观察器，无运行时DLL依赖。只读本机内嵌对象，原调用参数不变且恰好一次。</summary>
internal sealed unsafe class NativeAnimationObserver : IDisposable
{
    private delegate void Play(ActionTimelineSequencer* self, ushort id, void* args);
    private delegate void PlayAction(TimelineContainer* self, ushort intro, ushort loop, void* args);
    private delegate void SetSlot(ActionTimelineSequencer* self, uint slot, ushort id);
    private Hook<Play>? hook;
    private Hook<PlayAction>? actionHook;
    private Hook<SetSlot>? slotHook;
    [ThreadStatic] private static AnimationRoot scope;
    [ThreadStatic] private static uint requestedAction;
    internal readonly struct ActionScope : IDisposable
    {
        private readonly uint previous;
        public ActionScope(uint action) { previous=requestedAction; requestedAction=action; }
        public void Dispose() => requestedAction=previous;
    }
    private readonly record struct PostSlot(int Slot, AnimationRoot Root, SelfAnimationSample Sample, double Time);
    private readonly Channel<PostSlot> pending = Channel.CreateBounded<PostSlot>(new BoundedChannelOptions(256) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });
    private readonly Func<nint> actor;
    private readonly Func<bool> enabled;
    private readonly Action<AnimationPlayRequest> request;
    private readonly SelfAnimationTracker tracker;
    private readonly Action<int, PlaybackTracker.Snapshot, long>? rawSample;
    private nint localActor;
    private long roots, dropped, playCalls, localPlayCalls, actionCalls, localActionCalls, slotCalls, localSlotCalls;
    public bool PlayEnabled => hook?.IsEnabled == true;
    public long Dropped => Interlocked.Read(ref dropped);
    public object Diagnostics => new { playEnabled = PlayEnabled, actionEnabled = actionHook?.IsEnabled == true, slotEnabled = slotHook?.IsEnabled == true,
        playCalls, localPlayCalls, actionCalls, localActionCalls, slotCalls, localSlotCalls, dropped = Interlocked.Read(ref dropped), activeEpisodes = tracker.ActiveCount };
    private static double Now => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
    public NativeAnimationObserver(IGameInteropProvider interop, Func<nint> actor, Func<bool> enabled,
        Action<SelfAnimationSignal> signal, Action<AnimationPlayRequest> request, IPluginLog log,
        Action<int, PlaybackTracker.Snapshot, long>? rawSample = null)
    {
        this.actor = actor; this.enabled = enabled; this.request = request; this.rawSample = rawSample;
        localActor = actor(); tracker = new(signal);
        try { hook = interop.HookFromSignature<Play>(ActionTimelineSequencer.Addresses.PlayTimeline.String, OnPlay); hook.Enable(); }
        catch (Exception e) { log.Warning(e, "Native animation play observation unavailable"); }
        try { actionHook = interop.HookFromSignature<PlayAction>(TimelineContainer.Addresses.PlayActionTimeline.String, OnAction); actionHook.Enable(); }
        catch (Exception e) { log.Warning(e, "Native animation container observation unavailable"); }
        try { slotHook = interop.HookFromSignature<SetSlot>(ActionTimelineSequencer.Addresses.SetSlotTimeline.String, OnSlot); slotHook.Enable(); }
        catch (Exception e) { log.Warning(e, "Native animation slot observation unavailable"); }
    }
    private bool IsLocal(ActionTimelineSequencer* self)
    {
        var ptr = Volatile.Read(ref localActor);
        return enabled() && ptr != 0 && self != null && self == &((Character*)ptr)->Timeline.TimelineSequencer;
    }
    private void OnPlay(ActionTimelineSequencer* self, ushort id, void* args)
    {
        var time = Now; Interlocked.Increment(ref playCalls);
        var local = IsLocal(self); var previous = scope;
        var root = local ? scope.Id != 0 ? scope : new AnimationRoot(Interlocked.Increment(ref roots), id, 0, requestedAction) : default;
        var before = local ? Before(self) : null;
        if (local) scope = root;
        try { hook!.Original(self, id, args); } finally { scope = previous; }
        try { if (local) { Interlocked.Increment(ref localPlayCalls); request(new("play", time, Now, root, -1, id)); PostChanges(self, before, root); } } catch { }
    }
    private void OnAction(TimelineContainer* self, ushort intro, ushort loop, void* args)
    {
        var time = Now; Interlocked.Increment(ref actionCalls);
        var ptr = Volatile.Read(ref localActor);
        var local = enabled() && ptr != 0 && self == &((Character*)ptr)->Timeline;
        var previous = scope;
        var root = local ? new AnimationRoot(Interlocked.Increment(ref roots), intro, loop, requestedAction) : default;
        var before = local ? Before(&self->TimelineSequencer) : null;
        if (local) scope = root;
        try { actionHook!.Original(self, intro, loop, args); } finally { scope = previous; }
        try { if (local) { Interlocked.Increment(ref localActionCalls); request(new("container", time, Now, root, -1, intro)); PostChanges(&self->TimelineSequencer, before, root); } } catch { }
    }
    private static SelfAnimationSample Read(ActionTimelineSequencer* self, int slot, float overall)
    {
        var scheduler = self->GetSchedulerTimeline((uint)slot);
        float? cursor = scheduler == null ? null : scheduler->CurrentTimestamp;
        if (cursor.HasValue && !float.IsFinite(cursor.Value)) cursor = null;
        return new(self->TimelineIds[slot], (ulong)(nuint)scheduler, cursor, self->TimelineSpeeds[slot], overall);
    }
    private SelfAnimationSample[]? Before(ActionTimelineSequencer* self)
    {
        try
        {
            var ptr = Volatile.Read(ref localActor); if (ptr == 0) return null;
            var result = new SelfAnimationSample[14];
            for (var slot=0;slot<14;slot++) result[slot]=Read(self,slot,((Character*)ptr)->Timeline.OverallSpeed);
            return result;
        }
        catch { return null; } // 读观察值失败不能跳过原始调用。
    }
    private void PostChanges(ActionTimelineSequencer* self, SelfAnimationSample[]? before, AnimationRoot root)
    {
        if (before == null) return;
        var ptr = Volatile.Read(ref localActor); if (ptr == 0) return;
        for (var slot=0;slot<14;slot++)
        {
            var after=Read(self,slot,((Character*)ptr)->Timeline.OverallSpeed); var old=before[slot];
            if (old.Timeline==after.Timeline && old.Instance==after.Instance &&
                !(old.Cursor is {} previous && after.Cursor is {} current && current < previous-.001f)) continue;
            if (!pending.Writer.TryWrite(new(slot,root,after,Now))) Interlocked.Increment(ref dropped);
        }
    }
    private void OnSlot(ActionTimelineSequencer* self, uint slot, ushort id)
    {
        var time = Now; Interlocked.Increment(ref slotCalls);
        var root = scope;
        // 部分路径直接设置槽位，不经过PlayTimeline/PlayActionTimeline；仍保留UseAction原生调用栈证据。
        if (root.Id == 0 && requestedAction != 0) root = new(Interlocked.Increment(ref roots), id, 0, requestedAction);
        slotHook!.Original(self, slot, id);
        try
        {
            if (slot >= 14 || !IsLocal(self)) return;
            Interlocked.Increment(ref localSlotCalls);
            request(new("slot", time, Now, root, (int)slot, id));
            var ptr = Volatile.Read(ref localActor);
            var sample = Read(self, (int)slot, ((Character*)ptr)->Timeline.OverallSpeed);
            if (!pending.Writer.TryWrite(new((int)slot, root, sample, Now))) Interlocked.Increment(ref dropped);
        }
        catch { /* 观察失败不能改变原函数结果。 */ }
    }
    public void Sample()
    {
        var ptr = actor(); Volatile.Write(ref localActor, ptr);
        if (!enabled() || ptr == 0) return;
        while (pending.Reader.TryRead(out var item)) { tracker.Request(item.Slot, item.Root); tracker.Observe(item.Slot, item.Sample, item.Time); }
        var container = &((Character*)ptr)->Timeline; var sequence = &container->TimelineSequencer;
        for (var slot = 0; slot < 14; slot++)
        {
            var tick = Stopwatch.GetTimestamp(); var sample = Read(sequence, slot, container->OverallSpeed);
            tracker.Observe(slot, sample, tick / (double)Stopwatch.Frequency);
            rawSample?.Invoke(slot, new((ushort)sample.Timeline, sample.Instance, sample.Cursor, sample.Speed, sample.OverallSpeed), tick);
        }
    }
    public void Reset(string reason)
    {
        tracker.Close(Now, reason); while (pending.Reader.TryRead(out _)) { }
        Volatile.Write(ref localActor, actor());
    }
    public void Dispose() { hook?.Dispose(); actionHook?.Dispose(); slotHook?.Dispose(); pending.Writer.TryComplete(); }
}
