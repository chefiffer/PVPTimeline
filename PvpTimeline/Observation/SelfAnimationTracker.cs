namespace PvpSkillLab;

internal readonly record struct AnimationRoot(long Id, uint Intro, uint Loop, uint RequestedAction = 0);
internal sealed record AnimationPlayRequest(string Kind, double Before, double After, AnimationRoot Root,
    int Slot, uint Timeline, bool Local = true);
internal readonly record struct SelfAnimationSample(uint Timeline, ulong Instance, float? Cursor, float Speed, float OverallSpeed);
internal sealed record SelfAnimationSignal(long Episode, int Part, int Slot, uint Timeline, ulong Instance,
    AnimationRoot Root, double Time, double StartLower, double StartUpper, double ObservedThrough,
    double? EndLower, double? EndUpper, string State, string Reason, bool ProgressObserved,
    bool LeftCensored, bool RightCensored, long ReplacedByRoot = 0);

/// <summary>
/// 只依据本机调度对象/原生进度，不接收GCD、动作锁、伤害延迟或技能冷却。
/// 清空/替换是观测结束；停帧仅记为未推进，不宣称自然播完。原生游标单位不换算为秒。
/// </summary>
internal sealed class SelfAnimationTracker
{
    private sealed class Active
    {
        public long Episode;
        public int Part;
        public SelfAnimationSample Sample;
        public AnimationRoot Root;
        public double Lower, Upper, LastTime, Through, LastEmit = double.NegativeInfinity;
        public bool Progress, Left;
        public string State = "awaiting_progress";
    }
    private readonly Active?[] slots = new Active?[14];
    private readonly double[] lastSeen = new double[14];
    private readonly AnimationRoot[] pendingRoots = new AnimationRoot[14];
    private readonly Action<SelfAnimationSignal> emit;
    private long ordinal;
    public int ActiveCount => slots.Count(a => a != null);
    public SelfAnimationTracker(Action<SelfAnimationSignal> emit) => this.emit = emit;
    public void Request(int slot, AnimationRoot root) { if ((uint)slot < 14 && root.Id != 0) pendingRoots[slot] = root; }
    private void Send(int slot, Active a, double time, string reason, double? endLower = null, double? endUpper = null, bool right = false, long replacedBy = 0)
    {
        emit(new(a.Episode, a.Part, slot, a.Sample.Timeline, a.Sample.Instance, a.Root, time,
            a.Lower, a.Upper, a.Through, endLower, endUpper, a.State, reason, a.Progress, a.Left, right, replacedBy));
        a.LastEmit = time;
    }
    public void Observe(int slot, SelfAnimationSample sample, double time)
    {
        if ((uint)slot >= 14 || !double.IsFinite(time)) return;
        if (sample.Cursor is { } raw && !float.IsFinite(raw)) sample = sample with { Cursor = null };
        var old = slots[slot];
        var rewind = old != null && sample.Timeline == old.Sample.Timeline && sample.Instance == old.Sample.Instance &&
            old.Sample.Cursor is { } previous && sample.Cursor is { } current && current < previous - .001f;
        var changed = old != null && (sample.Timeline != old.Sample.Timeline || sample.Instance != old.Sample.Instance || rewind);
        if (changed)
        {
            var replacing = pendingRoots[slot];
            var reason = sample.Timeline == 0 || sample.Instance == 0 ? "slot_cleared_or_object_removed"
                : rewind ? "cursor_rewind_or_loop" : replacing.Id != 0 && replacing.Id != old!.Root.Id ? "replaced_by_local_action" : "slot_or_object_replaced";
            old!.State = "ended_observed";
            Send(slot, old, time, reason, old.LastTime, time, replacedBy: replacing.Id);
            slots[slot] = null;
        }
        if (sample.Timeline == 0 || sample.Instance == 0) { lastSeen[slot] = time; pendingRoots[slot] = default; return; }
        var a = slots[slot];
        if (a == null)
        {
            var root = pendingRoots[slot]; pendingRoots[slot] = default;
            a = new Active { Episode = ++ordinal, Sample = sample, Root = root,
                Lower = lastSeen[slot] == 0 ? time : lastSeen[slot], Upper = time, LastTime = time,
                Through = time, Left = lastSeen[slot] == 0 && root.Id == 0 };
            slots[slot] = a; Send(slot, a, time, "playback_object_observed");
        }
        else
        {
            var advances = sample.Cursor is { } cursor && a.Sample.Cursor is { } prior && cursor > prior + .00001f;
            var paused = sample.Speed <= 0 || sample.OverallSpeed <= 0;
            var next = advances && !paused ? "playing_observed" : paused ? "paused_observed"
                : time - a.Through >= .15 ? "not_advancing" : a.State;
            var transition = next != a.State;
            if (advances && !paused)
            {
                if (a.State is "paused_observed" or "not_advancing")
                { a.Part++; a.Lower = a.LastTime; a.Upper = time; a.Progress = false; }
                a.Through = time; a.Progress = true;
            }
            a.Sample = sample; a.State = next;
            if (transition || time - a.LastEmit >= .05) Send(slot, a, time, transition ? next : "progress_sample");
            a.LastTime = time;
        }
        lastSeen[slot] = time;
    }
    public void Close(double time, string reason)
    {
        for (var slot = 0; slot < 14; slot++)
        {
            if (slots[slot] is { } a) { a.State = "observation_ended"; Send(slot, a, time, reason, a.LastTime, time, true); }
            slots[slot] = null; pendingRoots[slot] = default; lastSeen[slot] = 0;
        }
    }
}
