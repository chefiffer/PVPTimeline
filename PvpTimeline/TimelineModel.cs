using PvpSkillLab;

namespace PvpTimeline;

internal sealed record ActionInfo(uint Id, string Name, uint Icon, uint Category, byte PrimaryGroup,
    byte AdditionalGroup, double StaticCast, double StaticRecast, uint AnimationStart, uint AnimationEnd)
{
    public bool IsMount=>Id==MountObservation.ActionId;
    // 类别与公CD归组分开：Spell/Weaponskill放下轨；LB等特殊动作也不冒充Ability。
    public bool Ability => Category == 4 && PrimaryGroup != 58 && AdditionalGroup != 58;
    public bool SharedGcd => Category is 2 or 3 || PrimaryGroup == 58 || AdditionalGroup == 58;
}

internal sealed class TimelineEntry
{
    public required ActionInfo Info;
    public long ReleaseId;
    public double FeedbackDeadline;
    public bool FeedbackExpired;
    public double Start;
    public double? Request;
    public double? ExecutionStart;
    public double? Ack;
    public ushort Sequence;
    public uint GlobalSequence;
    public ulong Target;
    public bool Queued;
    public bool TriggeredEffect;
    public double? ObservedEffectAt;
    public double? EmptyReturnConfirmation;
    public uint? ParentAction;
    public int AbilityLane;
    public bool Ambiguous;
    public byte ObservedDamageFlags;
    public double? CriticalAt,DirectAt;
    public byte DamageFlagsAt(double now)=>(byte)((CriticalAt<=now?0x20:0)|(DirectAt<=now?0x40:0));
    public uint DamageResultCount;
    public bool HasDirectResponse;
    public bool CastObserved;
    public bool CastSuperseded;
    public bool CastAbandoned=>Ack==null && (CastSuperseded || CastObserved && CastEnd is {} end && end<(ExecutionStart??Start)+CastDuration-.2);
    public double? CastEnd { get; set; }
    public double CastDuration;
    public double? GcdStart { get; set; }
    public double? GcdDuration { get; set; }
    public double? CooldownDuration { get; set; }
    public double? CooldownStart { get; set; }
    public bool DelayedPayloadClaimed;
    public double? LockStart;
    public double? LockEnd;
    public double ReportedLock;
    public bool LockReplaced;
    public readonly HashSet<uint> EffectActionIds = new();
    public bool MatchesAction(uint id) => Info.Id == id || EffectActionIds.Contains(id);
    public readonly List<EffectObservation> Effects = new(8);
    public readonly List<AnimationSpan> Animations = new(4);
}
internal sealed record EffectObservation(double Time, uint Target, byte Type, uint Value, byte DamageFlags = 0);
internal sealed class AnimationSpan
{
    public int Slot;
    public uint Timeline;
    public double Start;
    public double? End;
    public string EndReason = "still_observed";
    public string Evidence = "frame_observed";
    public double? MeasuredThrough;
    public bool ProgressObserved = true;
    public string State = "legacy_observation";
    public double? StartLower, EndLower, EndUpper;
    public bool LeftCensored, RightCensored;
    public long RootRequestId, ReplacedByRoot;
    public uint? ReplacedByAction;
}

/// <summary>
/// 只保留最近120秒且最多256次动作，每动作最多32个效果标记/32段动画；无磁盘历史、无网络。
/// 延迟包仅在同技能唯一候选时关联，多个候选标为歧义，绝不随意贴到最近一次释放。
/// 时间全部为同一Stopwatch时钟的秒数，不等同服务器内部判定时刻。
/// </summary>
internal sealed class TimelineModel
{
    private readonly List<TimelineEntry> entries = new(256);
    private long nextReleaseId;
    private readonly Dictionary<long,TimelineEntry> pendingReleases=new(128);
    private readonly Dictionary<ushort,TimelineEntry> sequenceOwners=new(128);
    private readonly Queue<TimelineEntry> retiredReleases=new(128);
    public int PendingCount=>pendingReleases.Count;
    public long ExpiredFeedbacks {get;private set;}
    private bool Pending(TimelineEntry e,double now)=>!e.CastAbandoned && !e.FeedbackExpired && !e.TriggeredEffect && now<=e.FeedbackDeadline && pendingReleases.ContainsKey(e.ReleaseId);
    private void Retire(TimelineEntry entry)
    {
        pendingReleases.Remove(entry.ReleaseId);entry.FeedbackExpired=true;
        if(sequenceOwners.TryGetValue(entry.Sequence,out var bound) && ReferenceEquals(bound,entry))sequenceOwners.Remove(entry.Sequence);
        if(!entry.HasDirectResponse && entry.EmptyReturnConfirmation==null)
        {if(retiredReleases.Count==128)retiredReleases.Dequeue();retiredReleases.Enqueue(entry);}
    }
    private void Expire(double now)
    {
        foreach(var e in pendingReleases.Values.Where(e=>now>e.FeedbackDeadline || e.CastAbandoned).ToArray())Retire(e);
        foreach(var seq in sequenceOwners.Where(p=>!pendingReleases.ContainsKey(p.Value.ReleaseId)).Select(p=>p.Key).ToArray())sequenceOwners.Remove(seq);
        while(retiredReleases.TryPeek(out var e) && now>e.FeedbackDeadline+14)retiredReleases.Dequeue();
    }
    private readonly Queue<object> rawEffectDiagnostics=new(128);
    private readonly Queue<object> pitchDiagnostics=new(64);
    public string EffectDiagnostics()=>System.Text.Json.JsonSerializer.Serialize(new{PendingCount,ExpiredFeedbacks,AmbiguousEffects,pitchPerfect=pitchDiagnostics.ToArray(),raw=rawEffectDiagnostics.ToArray(),entries=entries.Select(e=>new{action=e.Info.Id,e.ReleaseId,e.FeedbackDeadline,e.FeedbackExpired,e.Start,e.Ack,e.Sequence,e.GlobalSequence,e.ObservedDamageFlags,e.DamageResultCount,e.CastAbandoned,e.CastEnd,e.ExecutionStart,e.CastDuration,e.Ambiguous,e.TriggeredEffect,retained=e.Effects.Count})});
    private readonly Dictionary<uint, TimelineEntry> payloadOwners = new();
    private readonly HashSet<uint> ambiguousPackets = new();
    private readonly List<AnimationSpan> recentAnimations = new(128);
    private readonly TimelineEntry?[] abilityLanes = new TimelineEntry?[2];
    private int nextAbilityLane;
    private readonly Dictionary<long, AnimationPlayRequest> roots = new();
    private readonly Dictionary<long, TimelineEntry> rootOwners = new();
    private readonly Dictionary<(long Episode, int Part), (AnimationSpan Span, SelfAnimationSignal Signal)> nativeSpans = new();
    private readonly record struct GcdObservation(double Time, float Elapsed, float Total, uint Action);
    private readonly Queue<GcdObservation> recentGcd = new(512);
    public int UnmatchedAnimationCount => nativeSpans.Values.Count(s => !entries.Any(e => e.Animations.Contains(s.Span)));
    public IReadOnlyList<TimelineEntry> Entries => entries;
    public long AmbiguousEffects { get; private set; }
    public void Clear() { entries.Clear(); pendingReleases.Clear();sequenceOwners.Clear();retiredReleases.Clear();ExpiredFeedbacks=0; rawEffectDiagnostics.Clear(); pitchDiagnostics.Clear(); payloadOwners.Clear(); ambiguousPackets.Clear(); recentAnimations.Clear(); roots.Clear(); rootOwners.Clear(); nativeSpans.Clear(); recentGcd.Clear(); Array.Clear(abilityLanes); nextAbilityLane = 0; AmbiguousEffects = 0; }
    public void Prune(double now)
    {
        Expire(now);
        entries.RemoveAll(e => now - e.Start > 120);
        if (entries.Count > 256) entries.RemoveRange(0, entries.Count - 256);
        recentAnimations.RemoveAll(a => now - a.Start > 15 && a.End != null);
        if (recentAnimations.Count > 128) recentAnimations.RemoveRange(0, recentAnimations.Count - 128);
        foreach (var key in payloadOwners.Where(p => !entries.Contains(p.Value)).Select(p => p.Key).ToArray()) payloadOwners.Remove(key);
        foreach (var key in rootOwners.Where(p => !entries.Contains(p.Value)).Select(p => p.Key).ToArray()) rootOwners.Remove(key);
    }
    private TimelineEntry Add(ActionInfo info, double now, ulong target,bool passive=false)
    {
        Prune(now);
        if (entries.Count >= 256) {var removed=entries[0];if(pendingReleases.ContainsKey(removed.ReleaseId))Retire(removed);entries.RemoveAt(0);}
        var e = new TimelineEntry { Info = info, Start = now, Target = target,ReleaseId=++nextReleaseId,FeedbackDeadline=now+14,TriggeredEffect=passive };
        if(!passive)
        {
            if(pendingReleases.Count==128)Retire(pendingReleases.Values.MinBy(p=>p.ReleaseId)!);
            pendingReleases[e.ReleaseId]=e;
        }
        if (info.Ability)
        {
            var lane = Array.FindIndex(abilityLanes, previous => previous == null || now - previous.Start > 8 || previous.Effects.Any(f => f.Time <= now && CanMarkEffect(f.Type)));
            if (lane < 0) lane = nextAbilityLane; // 两排都在等待效果时轮换复用，不增长高度。
            e.AbilityLane = lane; abilityLanes[lane] = e; nextAbilityLane = 1 - lane;
        }
        entries.Add(e); return e;
    }
    public TimelineEntry Requested(ActionInfo info, double now, ulong target, bool queued)
    {
        Expire(now);
        var echo = entries.LastOrDefault(e => e.Info.Id == info.Id && e.Ack is { } ack && now - ack is >= 0 and < .12);
        if (echo != null) return echo;
        // 连点/排队重试可能持续整个读条，不用一秒阈值把同一次读条拆成多个请求。
        var existing = pendingReleases.Values.LastOrDefault(e => Pending(e,now) && e.CastEnd==null && e.Info.Id == info.Id && e.Ack == null && now - e.Start <= Math.Max(3, e.CastDuration + 2) && (uint)e.Target == (uint)target);
        if (existing != null)
        {
            if (!queued && existing.Queued) { existing.ExecutionStart = now; existing.Start = now; existing.Queued = false; }
            return existing;
        }
        var entry = Add(info, now, target); entry.Request = now; entry.Queued = queued;
        if (!queued) entry.ExecutionStart = now;
        AttachAnimations(entry); return entry;
    }
    public TimelineEntry Acknowledged(ActionInfo info, double now, ulong target, ushort sequence, uint global, float reportedLock)
    {
        Expire(now);
        // 同一释放可分成多个全局消息；本机来源序列才是同一次释放的去重依据。
        var duplicate = sequence!=0 && sequenceOwners.TryGetValue(sequence,out var bound) && Pending(bound,now)?bound:null;
        if (duplicate != null)
        {
            if (duplicate.EffectActionIds.Count < 8) duplicate.EffectActionIds.Add(info.Id);
            return duplicate;
        }
        var candidates = pendingReleases.Values.Where(e => Pending(e,now) && e.Info.Id == info.Id && e.Ack == null && now-e.Start>=0).ToArray();
        if (candidates.Length == 0)
            candidates = pendingReleases.Values.Where(e => Pending(e,now) && e.Ack == null && e.Info.IsMount==info.IsMount && e.Info.Ability == info.Ability && (uint)e.Target == (uint)target &&
                (e.CastObserved ? now - (e.CastEnd ?? e.Start + e.CastDuration) is >= -.35 and <= .75 : now - (e.ExecutionStart ?? e.Start) is >= 0 and <= .35)).ToArray();
        var entry = candidates.Length == 1 ? candidates[0] : Add(info, now, target);
        entry.Info = info; // 替换技能确认更新同一图标，不创建“命中后的第二个图标”。
        entry.Ambiguous = candidates.Length > 1;
        entry.Ack = now; entry.Sequence = sequence; entry.GlobalSequence = global; entry.Target = target;
        if(sequence!=0)sequenceOwners[sequence]=entry;
        entry.ReportedLock = Math.Clamp(float.IsFinite(reportedLock) ? reportedLock : 0, 0, 30);
        entry.LockStart = now;
        // 共享冷却可能先于确认到达。回看有限的实测样本，不靠技能说明补一个固定公CD。
        if (entry.Info.SharedGcd && entry.GcdDuration == null)
            foreach (var sample in recentGcd.Where(s => s.Time >= (entry.Request ?? entry.Start)-.15 && s.Time <= now+.05)
                         .OrderBy(s=>Math.Abs(s.Time-s.Elapsed-now)))
            { AssignSharedCooldown(sample.Time,sample.Elapsed,sample.Total,sample.Action); if (entry.GcdDuration != null) break; }
        // 新动作覆盖动作锁时，上一动作右截断；不能把连续动作锁当成前一技能超长硬直。
        foreach (var other in entries.Where(e => e != entry && e.LockStart != null && e.LockEnd == null))
        { other.LockEnd = now; other.LockReplaced = true; }
        AttachAnimations(entry);
        return entry;
    }
    public TimelineEntry Casting(ActionInfo info, double now, float elapsed, float total, ulong target)
    {
        Expire(now);
        // 效果确认可能早于框架清除IsCasting，复用刚确认的读条，避免尾帧再造一个图标。
        var observedStart=now-Math.Max(0,elapsed);
        foreach(var previous in entries.Where(e=>e.Info.Id==info.Id && e.CastObserved && e.Ack==null && Math.Abs((e.ExecutionStart??e.Start)-observedStart)>.35))previous.CastSuperseded=true;
        var entry = pendingReleases.Values.LastOrDefault(e => Pending(e,now) && e.Info.Id == info.Id && (e.Ack == null && e.CastEnd==null ||
            e.Ack is { } ack && (now - ack is >= 0 and <= .35 || e.CastObserved && e.ExecutionStart is {} begin && Math.Abs(begin-observedStart)<=.2)))
            ?? Add(info, now - Math.Max(0, elapsed), target);
        entry.Start = Math.Min(entry.Start, now - Math.Max(0, elapsed));
        entry.ExecutionStart = now - Math.Max(0, elapsed);
        entry.CastObserved = true; entry.CastDuration = total; AttachAnimations(entry); return entry;
    }
    /// <summary>按共享冷却启动时刻关联，兼容运行时组保留基础技能ID/返回0的替换技能。旧周期不借给新技能。</summary>
    public void SharedCooldown(double now, float elapsed, float total, uint runtimeAction)
    {
        if (!float.IsFinite(elapsed) || !float.IsFinite(total) || total <= 0 || elapsed < 0 || elapsed > total) return;
        recentGcd.Enqueue(new(now,elapsed,total,runtimeAction));
        while (recentGcd.Count>512 || now-recentGcd.Peek().Time>10) recentGcd.Dequeue();
        AssignSharedCooldown(now,elapsed,total,runtimeAction);
    }
    private void AssignSharedCooldown(double now,float elapsed,float total,uint runtimeAction)
    {
        var start = now - elapsed;
        // 一个已认领的冷却周期不再分配给另一个排队请求。
        if (entries.Any(e => e.GcdStart is {} known && e.GcdDuration is {} duration && Math.Abs(duration-total)<.05 &&
            (Math.Abs(known-start)<=.08 || runtimeAction==0 && elapsed<=.001f && start>known && start<known+duration-.1))) return;
        var candidates = entries.Where(e => e.Info.SharedGcd && e.GcdDuration==null &&
            (!e.Queued || e.Ack!=null || e.CastObserved) &&
            (Math.Abs((e.ExecutionStart ?? e.Start)-start)<=.35 || e.Ack is {} ack && Math.Abs(ack-start)<=.35)).ToArray();
        // 同一启动时刻有多个候选时，ID只作为消歧信息，不作为必须相等的前提。
        if (candidates.Length > 1) candidates = candidates.Where(e => e.Info.Id == runtimeAction).ToArray();
        if (candidates.Length != 1) return;
        var entry = candidates[0];
        if (entry.GcdStart is { } previous && Math.Abs(previous - start) > .35) return;
        if (entry.GcdDuration == null) { entry.GcdStart = start; entry.GcdDuration = total; }
    }
    // 非零类型均保留为已观测效果；未知类型不会被直接解释成伤害或治疗量。
    public static bool IsObservableEffect(byte type) => type != 0;
    // 消耗资源、连击状态等内部反馈不是命中；尤其27会随确认先到，不能拿来画目标效果。
    public static bool CanMarkEffect(byte type) => type is 3 or 4 or 5 or 6 or 14 or 15 or 17 or 18 or 32 or 33 or 34 or 39 or 59 or 61 or 73 or 75;
    public void OwnCooldown(uint action,double now,float elapsed,float total)
    {
        Expire(now);
        if (!float.IsFinite(elapsed) || !float.IsFinite(total) || total<=0 || elapsed<0 || elapsed>total) return;
        var start=now-elapsed;
        if(entries.Any(e=>e.Info.Id==action && e.CooldownStart is {} existing && Math.Abs(existing-start)<.08)) return;
        var candidates=pendingReleases.Values.Where(e=>Pending(e,now) && e.Info.Id==action && e.CooldownDuration==null &&
            (!e.Queued || e.Ack!=null) && (Math.Abs((e.ExecutionStart ?? e.Start)-start)<=.35 || e.Ack is {} ack && Math.Abs(ack-start)<=.35)).ToArray();
        if(candidates.Length!=1)return;
        candidates[0].CooldownStart=start;candidates[0].CooldownDuration=total;
    }
    /// <summary>
    /// Accept one observed effect row. Times are monotonic seconds; IDs and flags are raw game data.
    /// Resolve only a bound packet/sequence or a unique reviewed parent. Uncertainty must not
    /// overwrite another release. The method mutates bounded history and never controls gameplay.
    /// </summary>
    public void Effect(ActionInfo info, double now, uint target, byte type, uint value, ushort sequence, uint global, byte damageFlags = 0)
    {
        Expire(now);
        if(rawEffectDiagnostics.Count==128)rawEffectDiagnostics.Dequeue();
        var diagnostic=new{action=info.Id,time=now,type,sequence,global,flags=damageFlags,value,target};
        rawEffectDiagnostics.Enqueue(diagnostic);
        if(info.Id==29392){if(pitchDiagnostics.Count==64)pitchDiagnostics.Dequeue();pitchDiagnostics.Enqueue(diagnostic);}
        TimelineEntry? entry = null;
        if(sequence!=0 && !sequenceOwners.ContainsKey(sequence) && retiredReleases.Any(e=>now<=e.FeedbackDeadline+14 && e.Sequence==sequence && e.MatchesAction(info.Id)))
        {ExpiredFeedbacks++;return;}
        // 来源序列为零的迟到包无法证明属于新的同名释放；短期墓碑防止串单。
        if(sequence==0 && !(payloadOwners.TryGetValue(global,out var knownPacket) && Pending(knownPacket,now)) && retiredReleases.Any(e=>now<=e.FeedbackDeadline+14 && e.Ack!=null && (e.MatchesAction(info.Id) || TriggerAction(info.Id)==e.Info.Id)))
        {ExpiredFeedbacks++;return;}
        if (ambiguousPackets.Contains(global)) return;
        if(sequence!=0 && sequenceOwners.TryGetValue(sequence,out var exact) && Pending(exact,now) && exact.MatchesAction(info.Id))entry=exact;
        else if (payloadOwners.TryGetValue(global, out var owner) && owner.MatchesAction(info.Id) && (owner.TriggeredEffect || Pending(owner,now))) entry = owner;
        else
        {
            var candidates = pendingReleases.Values.Where(e => Pending(e,now) && e.MatchesAction(info.Id) && e.Ack != null && now-e.Ack>=0 &&
                (sequence == 0 ? !e.DelayedPayloadClaimed : e.Sequence == sequence)).ToArray();
            // 原包确认：手动续键和实际伤害使用不同Action ID。只关联已核实的唯一手动释放。
            if(candidates.Length==0 && TriggerAction(info.Id) is {} trigger)
                candidates=pendingReleases.Values.Where(e=>Pending(e,now) && e.Info.Id==trigger && e.Ack!=null && (!e.DelayedPayloadClaimed || info.Id is 29706 or 29410 or 29411 or 29603 or 29498) && now-e.Ack>=0).ToArray();
            if(candidates.Length==0 && sequence==0)
            {
                // 同一次释放可能分开送达状态与伤害。仅有唯一同技能释放时可继续收集，
                // 不因第一阶段已到达就再造一个图标；重复释放仍保留歧义边界。
                var stages=pendingReleases.Values.Where(e=>Pending(e,now) && e.MatchesAction(info.Id) && e.Ack!=null && now-e.Ack>=0).ToArray();
                if(stages.Length==1)candidates=stages;
            }
            if (candidates.Length > 1)
            {
                AmbiguousEffects++;
                if (ambiguousPackets.Count >= 256) ambiguousPackets.Clear();
                ambiguousPackets.Add(global); return;
            }
            if (candidates.Length==1) entry=candidates[0];
            else
            {
                // 先尝试唯一尚未确认的本机请求；仍无对应项时，把实际收到的自身触发效果独立保留。
                var pending=pendingReleases.Values.Where(e=>Pending(e,now) && e.Ack==null && e.MatchesAction(info.Id) && now-e.Start>=0).ToArray();
                if(pending.Length==0 && TriggerAction(info.Id) is {} pendingTrigger)
                    pending=pendingReleases.Values.Where(e=>Pending(e,now) && e.Ack==null && e.Info.Id==pendingTrigger && now-e.Start>=0).ToArray();
                if(pending.Length>1) { AmbiguousEffects++;return; }
                if(pending.Length==1) entry=pending[0];
                else
                {
                    if(!CanMarkEffect(type)) return;
                    entry=entries.LastOrDefault(e=>e.TriggeredEffect && e.MatchesAction(info.Id) && now-(e.ObservedEffectAt ?? e.Start) is >=0 and <=.05);
                    if(entry==null)
                    {
                        entry=Add(info,now,target,true);entry.ObservedEffectAt=now;
                        // 仅这一对已经由真实原包核对；其他触发阶段不猜原技能。
                        if(info.Id==29068)
                        {
                            var parents=entries.Where(e=>e.Info.Id==29067 && e.Ack is {} ack && now-ack is >=0 and <=30).ToArray();
                            if(parents.Length==1)entry.ParentAction=29067;
                        }
                        AttachAnimations(entry);
                    }
                }
            }
            // 非效果行不能提前吃掉这次释放的延迟包名额。
            if (sequence == 0 && CanMarkEffect(type)) entry.DelayedPayloadClaimed = true;
            if(entry.Info.Id!=info.Id && entry.EffectActionIds.Count<8) entry.EffectActionIds.Add(info.Id);
            if (payloadOwners.Count >= 256) payloadOwners.Remove(payloadOwners.Keys.First());
            payloadOwners[global] = entry;
        }
        // 直暴结论独立于详情列表容量/去重；每条已关联结果都参与，只合并标志不猜命中。
        if(TimelinePresentation.DirectImpact(type))entry.HasDirectResponse=true;
        if(type is 3 or 4 or 5 or 6)
        {
            entry.ObservedDamageFlags|=(byte)(damageFlags&0x60);entry.DamageResultCount++;
            if((damageFlags&0x20)!=0)entry.CriticalAt??=now;
            if((damageFlags&0x40)!=0)entry.DirectAt??=now;
        }
        if (entry.Effects.Count < 32 && !entry.Effects.Any(e => e.Time == now && e.Target == target && e.Type == type && e.Value == value))
            entry.Effects.Add(new(now, target, type, value, damageFlags));
    }
    // Reviewed effect-to-manual relations. Sky Shatter binds to descent, never to takeoff;
    // automatic landing without a manual descent remains an observed passive result.
    private static uint? TriggerAction(uint id)=>id switch {29096=>29095,39187=>39186,29558=>29422,29426=>29470,29706=>29260,29410 or 29411=>41470,29603=>41457,29498=>29499,_=>null};
    public void EmptyPayload(uint action,double now)
    {
        Expire(now);
        // Regress原包没有目标效果行。只保留服务端返回确认，空心标记不声称伤害/状态命中。
        if(!TimelinePresentation.MovementReceipt(action))return;
        var candidates=pendingReleases.Values.Where(e=>Pending(e,now) && e.Info.Id==action && e.Ack is {} ack && now-ack is >=0 and <=2 && e.EmptyReturnConfirmation==null).ToArray();
        if(candidates.Length==1)candidates[0].EmptyReturnConfirmation=now;
    }
    public void SampleLock(double now, float remaining)
    {
        if (!float.IsFinite(remaining) || remaining > .0001f) return;
        foreach (var e in entries.Where(e => e.LockStart != null && e.LockEnd == null)) e.LockEnd = now;
    }
    public void Animation(int slot, uint previous, uint current, double now, string reason)
    {
        if (reason is "speed_changed" or "initial_snapshot") return;
        foreach (var span in recentAnimations.Where(a => a.Slot == slot && a.End == null))
        {
            if (span.Timeline == current && reason == "slot_request") return; // Hook/帧采样对同一开始去重。
            if (span.Timeline == current && span.Evidence == "slot_request" && now - span.Start < .1 && reason == "timeline_replaced")
            { span.Evidence = "slot_request_and_frame"; return; }
            span.End = now; span.EndReason = reason;
        }
        if (current == 0 || reason is "slot_cleared" or "playback_object_removed") return;
        var animation = new AnimationSpan { Slot = slot, Timeline = current, Start = now,
            Evidence = reason == "slot_request" ? "slot_request" : "frame_observed" };
        if (recentAnimations.Count >= 128) recentAnimations.RemoveAt(0);
        recentAnimations.Add(animation);
        var candidates = entries.Where(e => MatchesAnimation(e, animation)).ToArray();
        if (candidates.Length == 1) candidates[0].Animations.Add(animation);
    }
    private static bool MatchesAnimation(TimelineEntry e, AnimationSpan a) => e.Animations.Count < 32 &&
        !e.Animations.Contains(a) && InActionPhase(e, a.Start) &&
        (e.Info.AnimationStart == a.Timeline || e.Info.AnimationEnd == a.Timeline);
    private static bool InActionPhase(TimelineEntry e, double time)
    {
        if(e.CastAbandoned)return false;
        var start = e.ExecutionStart ?? e.Request ?? e.Start;
        return time >= start - .15 && time <= Math.Max(e.Ack ?? start, e.CastEnd ?? start + e.CastDuration) + 1.5;
    }
    private void AttachAnimations(TimelineEntry e)
    {
        foreach (var a in recentAnimations.Where(a => MatchesAnimation(e, a)))
            if (!entries.Any(other => other != e && other.Animations.Contains(a))) e.Animations.Add(a);
        BindNativeAnimations();
    }
    public void NativeRequest(AnimationPlayRequest request)
    {
        if (request.Root.Id == 0) return;
        if (roots.Count >= 64 && !roots.ContainsKey(request.Root.Id))
        { var key = roots.Keys.First(); roots.Remove(key); rootOwners.Remove(key); }
        // 保留根调用而不是由内部slot调用覆盖其开始时刻。
        if (!roots.ContainsKey(request.Root.Id) || request.Kind is "container" or "play") roots[request.Root.Id] = request;
        BindNativeAnimations();
    }
    public void NativeAnimation(SelfAnimationSignal signal)
    {
        var key = (signal.Episode, signal.Part);
        if (!nativeSpans.TryGetValue(key, out var value))
        {
            if (nativeSpans.Count >= 256)
            {
                var oldest = nativeSpans.Keys.First();
                var expired = nativeSpans[oldest].Span;
                if (expired.End == null) { expired.End = expired.MeasuredThrough ?? expired.Start; expired.RightCensored = true; expired.EndReason = "bounded_history_evicted"; }
                nativeSpans.Remove(oldest);
            }
            value = (new AnimationSpan { Slot = signal.Slot, Timeline = signal.Timeline, Start = signal.StartUpper }, signal);
        }
        var span = value.Span;
        span.MeasuredThrough = signal.ObservedThrough; span.ProgressObserved = signal.ProgressObserved;
        span.State = signal.State; span.StartLower = signal.StartLower; span.EndLower = signal.EndLower; span.EndUpper = signal.EndUpper;
        span.LeftCensored = signal.LeftCensored; span.RightCensored = signal.RightCensored;
        span.RootRequestId = signal.Root.Id; span.ReplacedByRoot = signal.ReplacedByRoot;
        span.Evidence = "native_object_and_cursor;not_first_rendered_frame";
        span.EndReason = signal.Reason;
        if (signal.EndUpper != null || signal.State is "paused_observed" or "not_advancing")
            span.End = Math.Max(span.Start, signal.ObservedThrough);
        nativeSpans[key] = (span, signal);
        BindNativeAnimations();
    }
    private void BindNativeAnimations()
    {
        foreach (var pair in roots)
        {
            if (rootOwners.ContainsKey(pair.Key)) continue;
            var r = pair.Value;
            var candidates = entries.Where(e => InActionPhase(e, r.Before) &&
                (e.Info.AnimationStart != 0 && (e.Info.AnimationStart == r.Root.Intro || e.Info.AnimationStart == r.Root.Loop) ||
                 e.Info.AnimationEnd != 0 && (e.Info.AnimationEnd == r.Root.Intro || e.Info.AnimationEnd == r.Root.Loop) ||
                 r.Root.RequestedAction != 0 && e.MatchesAction(r.Root.RequestedAction))).ToArray();
            if (r.Root.RequestedAction != 0)
            {
                var direct = candidates.Where(e => e.MatchesAction(r.Root.RequestedAction) &&
                    Math.Abs(r.Before-(e.ExecutionStart ?? e.Request ?? e.Start)) <= .35).OrderByDescending(e=>e.ExecutionStart ?? e.Request ?? e.Start).ToArray();
                if (direct.Length > 0) candidates = [direct[0]];
            }
            if (candidates.Length == 1) rootOwners[pair.Key] = candidates[0];
        }
        foreach (var item in nativeSpans.Values)
        {
            var span = item.Span; var signal = item.Signal;
            TimelineEntry? owner = null;
            if (signal.Root.Id != 0) rootOwners.TryGetValue(signal.Root.Id, out owner);
            if (owner == null && !signal.LeftCensored)
            {
                var candidates = entries.Where(e => InActionPhase(e, span.Start) &&
                    (e.Info.AnimationStart == span.Timeline || e.Info.AnimationEnd == span.Timeline)).ToArray();
                if (candidates.Length == 1) owner = candidates[0];
            }
            if (owner != null && owner.Animations.Count < 32 && !owner.Animations.Contains(span) &&
                !entries.Any(e => e != owner && e.Animations.Contains(span))) owner.Animations.Add(span);
            if (owner != null && rootOwners.TryGetValue(span.ReplacedByRoot, out var replacement))
            {
                if (replacement != owner) { span.ReplacedByAction = replacement.Info.Id; span.EndReason = "covered_by_action"; }
                else span.EndReason = "same_action_phase_transition";
            }
        }
    }
}
