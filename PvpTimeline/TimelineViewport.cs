namespace PvpTimeline;

/// <summary>固定相对刻度；Now只用于投影事件，不参与网格位置或标签的生成。</summary>
internal readonly record struct TimelineViewport(float Extent, float Scale, float Future, float History, bool Reverse)
{
    public float Present => Math.Max(0, Extent - Future * Scale);
    public double FirstOffset => -Math.Min(History, Present / Scale);
    public float OffsetPosition(double offset)
    {
        var x = Present + (float)offset * Scale;
        return Reverse ? Extent - x : x;
    }
    public float EventPosition(double time, double now) => OffsetPosition(time - now);
    public IEnumerable<double> Ticks(int subdivisions)
    {
        var first = (int)Math.Ceiling(FirstOffset * subdivisions);
        var last = (int)Math.Floor(Future * subdivisions);
        for (var i = first; i <= last; i++) yield return i / (double)subdivisions;
    }
}
