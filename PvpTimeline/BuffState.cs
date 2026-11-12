namespace PvpTimeline;

internal sealed record BuffReading(uint Id,uint Source,float Remaining,ushort Parameter);
internal sealed record BuffInfo(uint Id,string Name,string Description,uint Icon,bool Harmful=false);
internal sealed record BuffChange(double Time,float Remaining,ushort Parameter,bool Refresh);
internal sealed record BuffPlacement(BuffSpan Span,double Left,double End,bool Queued,string Counter);
internal static class BuffLayout
{
    public static bool MarkEndpoint(BuffSpan span,double now)=>!span.Gap && (span.End!=null || span.At(now) is {Remaining:>0});
    public static bool ExcludedRoleEffect(uint id)=>id is 4476 or 4480 or 4481 or 4482;
    // 跨职业统一识别允许状态；当前职业严格筛选时也保留队友给予的这些状态。
    public static bool AllowedPositive(uint id)=>id is
        4315 or 4317 or 5531 or 3226 or 3230 or 2282 or 4119 or 4117 or 4110 or 4109 or 3158 or 3149 or 4479 or 3161 or 2052 or 2022 or 4314 or
        // 近战：蛇祖/蛇行/蛇鳞；活性纹的盾与恢复；回返窗口；武士；忍者；龙骑；武僧。
        4094 or 4095 or 4096 or 4097 or 2861 or 2862 or 2860 or 1240 or 1320 or
        2010 or 3186 or 3189 or 4304 or 1316 or 3192 or 3177 or 3180 or 1310 or 3171 or 3170 or
        // 白魔/学者，源与受益者使用不同状态ID时均保留；不把鼓舞盾3087当作激励3088。
        1415 or 2053 or 2037 or 2038 or 3088 or 3092 or 3093 or 3096 or
        // 坦克：角色铁壁、圣盾阵(用户明确允许盾状态)、无敌、施护与受护、列阵、血气/解放/战栗。
        4168 or 1978 or 3026 or 1302 or 1300 or 1301 or 3210 or 3030 or 1303 or 3185 or
        3039 or 3037 or 3051 or 4295 or 4296 or 3052 or
        // 通用职能增益与PvP疾跑；明确排除职业选择标识。
        1982 or 1342 or 4477 or 4478 or
        3118 or 3119 or 3107 or 3110 or 3111 or 3099 or 1452 or 4330 or 4331 or 3104 or 3105;
    public static bool FilterPositive(uint id,uint job)=>!ExcludedRoleEffect(id) && (AllowedPositive(id) || job is not (19 or 21 or 32 or 37 or 20 or 22 or 30 or 34 or 39 or 41 or 24 or 28 or 25 or 27 or 35 or 42 or 31 or 38 or 33 or 40));
    // 如龙骑死者之岸同时增加输出与承伤，依用户指定放增益行，不仅靠StatusCategory判断。
    public static bool IsBenefit(uint id,BuffInfo? info)=>AllowedPositive(id) || info?.Harmful!=true;
    public static BuffPlacement[] Place(IEnumerable<BuffSpan> spans,double now,double first,double pixelsPerSecond,double iconPixels=16)
    {
        double End(BuffSpan s)=>s.End is {} ended && ended<=now?ended:s.At(now) is {Remaining:>0} c?c.Time+c.Remaining:s.Through;
        var queue=spans.Where(s=>s.Start<first).OrderByDescending(End).ThenBy(s=>s.Source).ToArray();
        return spans.Select(s=>
        {
            var rank=Array.IndexOf(queue,s);var queued=rank>=0;
            var left=queued?first+rank*(iconPixels*1.125)/pixelsPerSecond:s.Start;var end=End(s);
            var seconds=s.RemainingAt(now);
            var counter=queued && seconds is >0?Math.Min(99,Math.Ceiling(seconds.Value)).ToString("0"):"";
            return new BuffPlacement(s,left,end,queued,counter);
        }).Where(p=>p.End-p.Left>iconPixels/pixelsPerSecond).ToArray();
    }
}
internal sealed class BuffSpan(uint id,uint source,double start,string sourceName)
{
    public uint Id=id,Source=source;
    public double Start=start,Through=start;
    public double? End;
    public bool Gap;
    public string SourceName=sourceName;
    public List<BuffChange> Changes=new(4);
    public BuffChange? At(double time)=>Changes.LastOrDefault(c=>c.Time<=time);
    public double? RemainingAt(double time)
    {var change=At(time);return change is {Remaining:>0} && float.IsFinite(change.Remaining)?Math.Max(0,change.Time+change.Remaining-time):null;}
}
internal sealed record OwnBuff(BuffReading Reading,double FirstSeen,double ObservedAt,string SourceName)
{
    public float? Remaining(double now) => float.IsFinite(Reading.Remaining) && Reading.Remaining>0
        ? Math.Max(0,Reading.Remaining-(float)Math.Max(0,now-ObservedAt)) : null;
}

/// <summary>只维护本人的最新状态快照。没有倒数不等于已结束；缺少来源倒数不从其他计时器补齐。</summary>
internal sealed class BuffState
{
    // XIVAPI Status说明为“has been selected as a PvP role action”的已核对ID。
    // 这是装备/选择标识，不是技能生效。不能靠Remaining==0排除，否则会误删真实无倒数光环。
    internal static bool RoleSelectionMarker(uint id)=>id is 4483 or 4484 or 4485 or 4486 or 4487 or 4488 or 4489 or 4490 or 4491 or 4492 or 4494 or 4495 or 4496 or 4497 or 4516;
    private Dictionary<(uint Id,uint Source,int Ordinal),OwnBuff> states=new();
    private uint actor;
    private readonly List<BuffSpan> history=new(256);
    private readonly Dictionary<(uint,uint,int),BuffSpan> active=new();
    public IReadOnlyList<BuffSpan> History=>history;
    public BuffSpan[] Snapshot()=>history.Select(s=>
    {var copy=new BuffSpan(s.Id,s.Source,s.Start,s.SourceName){Through=s.Through,End=s.End,Gap=s.Gap};copy.Changes.AddRange(s.Changes);return copy;}).ToArray();
    public IEnumerable<OwnBuff> Current=>states.Values;
    public int Count=>states.Count;
    public double LastSnapshot {get;private set;}
    public void Clear() { states=new();actor=0;LastSnapshot=0;history.Clear();active.Clear(); }
    public void Observe(uint local,double now,IEnumerable<BuffReading> readings,Func<uint,string> sourceName)
    {
        if(local==0){Clear();return;}
        if(actor!=local)Clear();actor=local;
        if(LastSnapshot>0 && now-LastSnapshot>1)
        {
            foreach(var span in active.Values){span.End=span.Through;span.Gap=true;}
            active.Clear();
        }
        var next=new Dictionary<(uint,uint,int),OwnBuff>();
        var duplicates=new Dictionary<(uint,uint),int>();
        foreach(var reading in readings.Take(64))
        {
            if(reading.Id==0 || RoleSelectionMarker(reading.Id) || BuffLayout.ExcludedRoleEffect(reading.Id) || reading.Id is 3137 or 3142 or 2692 or 3204 or 3193 or 3194 or 3195 or 3196 or 3197 or 3198)continue;
            var identity=(reading.Id,reading.Source);duplicates.TryGetValue(identity,out var ordinal);duplicates[identity]=ordinal+1;
            var key=(reading.Id,reading.Source,ordinal);
            var first=states.TryGetValue(key,out var previous)?previous.FirstSeen:now;
            next[key]=new(reading,first,now,reading.Source==local?"自身 / self":sourceName(reading.Source));
            if(!active.TryGetValue(key,out var span))
            {
                span=new(reading.Id,reading.Source,now,next[key].SourceName);active[key]=span;history.Add(span);
            }
            var last=span.Changes.LastOrDefault();
            var refresh=last!=null && reading.Remaining>0 && last.Remaining>0 && now+reading.Remaining>last.Time+last.Remaining+.3;
            if(last==null || refresh || last.Parameter!=reading.Parameter || (last.Remaining>0)!=(reading.Remaining>0))
            {
                if(span.Changes.Count==128)span.Changes.RemoveAt(0);
                span.Changes.Add(new(now,reading.Remaining,reading.Parameter,refresh));
            }
            span.Through=now;
        }
        foreach(var key in active.Keys.Where(k=>!next.ContainsKey(k)).ToArray())
        {active[key].End=now;active.Remove(key);}
        history.RemoveAll(s=>(s.End ?? s.Through)<now-120);
        while(history.Count>256){var removed=history[0];history.RemoveAt(0);foreach(var key in active.Where(p=>p.Value==removed).Select(p=>p.Key).ToArray())active.Remove(key);}
        states=next;LastSnapshot=now;
    }
}
