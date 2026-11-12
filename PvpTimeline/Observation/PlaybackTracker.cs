namespace PvpSkillLab;

/// <summary>纯状态机：播放对象更换、同 ID 重播、时钟回退与槽位清空分开记录；绝不猜自然结束。</summary>
internal sealed class PlaybackTracker
{
    internal readonly record struct Snapshot(ushort Id, ulong Instance, float? Cursor, float Speed, float OverallSpeed);
    internal readonly record struct Change(int Slot, string Event, Snapshot Previous, Snapshot Current,
        long LowerTicks, long UpperTicks, bool LeftCensored);
    private readonly Dictionary<int, (Snapshot State, long Tick)> previous = new();
    public IReadOnlyList<Change> Observe(int slot, Snapshot current, long tick)
    {
        if (!previous.TryGetValue(slot, out var old))
        { previous[slot] = (current, tick); return [new(slot, "initial_snapshot", default, current, tick, tick, true)]; }
        string? kind = null;
        if (current.Id != old.State.Id) kind = current.Id == 0 ? "slot_cleared" : "timeline_replaced";
        else if (current.Instance != old.State.Instance) kind = current.Instance == 0 ? "playback_object_removed" : "playback_object_changed";
        else if (current.Cursor.HasValue && old.State.Cursor.HasValue && current.Cursor < old.State.Cursor - .001f) kind = "cursor_rewind_or_loop";
        else if (Math.Abs(current.Speed-old.State.Speed) > .0001f || Math.Abs(current.OverallSpeed-old.State.OverallSpeed) > .0001f) kind = "speed_changed";
        previous[slot] = (current, tick);
        return kind == null ? [] : [new(slot, kind, old.State, current, old.Tick, tick, false)];
    }
}
