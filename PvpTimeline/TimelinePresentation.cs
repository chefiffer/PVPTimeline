namespace PvpTimeline;

/// <summary>将重叠/相接的已观测动画区间合并；保留真实空隙，不靠补线伪造动画。</summary>
internal static class TimelinePresentation
{
    public static float BarY(bool ability,float row,float iconSize,float gcdOffset,float abilityOffset,float scale)
        =>row+iconSize*.78f+(ability?abilityOffset:gcdOffset)*scale;
    public static float BarThickness(bool ability,float gcdWidth,float abilityWidth,float scale)=>(ability?abilityWidth:gcdWidth)*scale;
    // 黄色锁条放在蓝条下半部，始终保留可见的蓝色边框与上半部。
    public static (float Offset,float Thickness) LockStripe(float barThickness) => (barThickness*.25f,Math.Max(.5f,barThickness*.35f));
    public static bool DirectImpact(byte type)=>type is 3 or 4 or 5 or 6 or 14 or 15 or 73;
    public static EffectObservation? PrimaryEffect(TimelineEntry entry,double now) => entry.Effects.Where(f=>f.Time<=now && TimelineModel.CanMarkEffect(f.Type) &&
        (!(entry.Info.Id is 41457 or 29603 or 41470 or 29410 or 29411) || DirectImpact(f.Type)))
        .OrderBy(f=>f.Time).ThenBy(f=>DirectImpact(f.Type)?0:1).FirstOrDefault()
        ?? (entry.EmptyReturnConfirmation is {} receipt && receipt<=now ? new EffectObservation(receipt,0,0,0) : null);
    public static bool ShortRecastAction(uint id)=>id is 29711 or 29091;
    public static uint DisplayAction(uint id)=>id switch {29068=>29067,29096=>29095,41477=>41476,29558=>29422,29706=>29260,29410 or 29411=>41470,29603=>41457,_=>id};
    public static bool ShowEntry(TimelineEntry entry)=>!(entry.TriggeredEffect && entry.Info.Id==41477);
    public static bool MovementReceipt(uint id)=>id is 29551 or 29550 or 29660 or 29261;
    public static bool RecuperateRecast(TimelineEntry entry)=>ShortRecastAction(entry.Info.Id) && !entry.TriggeredEffect && entry.Info.Ability && entry.CooldownStart!=null && entry.CooldownDuration is >0 and <=1.1;
    // 图标的时间锚点与条起点一致，不再插入一个导致左端漏出的像素偏移。
    public static double IconTime(double anchor)=>anchor;
    /// <summary>仅标记仍有近期推进证据、且原生调用确认被另一技能覆盖的片段；普通尾部不加符号。</summary>
    public static bool ShowInterruption(AnimationSpan span) => span.EndReason == "covered_by_action" &&
        span.ReplacedByAction != null && span.ProgressObserved && !span.RightCensored &&
        span.MeasuredThrough is { } through && span.EndUpper is { } upper && upper >= through && upper - through <= .15;
    public static IReadOnlyList<(double Start, double End)> AnimationBands(IEnumerable<AnimationSpan> spans, double now)
    {
        var result = new List<(double Start, double End)>();
        foreach (var span in spans.Where(a => a.ProgressObserved).Select(a => (Start: a.Start, End: Math.Min(a.End ?? a.MeasuredThrough ?? now, now)))
                     .Where(a => double.IsFinite(a.Start) && double.IsFinite(a.End) && a.End > a.Start).OrderBy(a => a.Start))
        {
            if (result.Count > 0 && span.Start <= result[^1].End + .000001)
                result[^1] = (result[^1].Start, Math.Max(result[^1].End, span.End));
            else result.Add(span);
        }
        return result;
    }
}
