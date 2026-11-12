namespace PvpTimeline;

internal sealed class PvpAreaResolver
{
    private double nextProbe;
    public uint Duty { get; private set; }
    public bool Eligible { get; private set; }
    public bool Pending { get; private set; } = true;
    // Constant-size state only: no tasks, timers, queues, or per-frame allocations.
    public void Invalidate() { Duty=0; Eligible=false; Pending=true; nextProbe=0; }
    public void Update(double now, uint territory, bool ready, Func<uint> readDuty)
    {
        if(!ready){Invalidate();return;}
        if(!Pending)return;
        // Open-world areas and the Den need no native instance query at all.
        if(territory==250 || !PvpAreaPolicy.IsMatchTerritory(territory))
        { Duty=0; Eligible=territory==250; Pending=false; return; }
        if(now<nextProbe)return;
        nextProbe=now+.2;
        Duty=readDuty();
        if(Duty==0)return; // Loading: retry later, never cache a missing ID as final.
        Eligible=PvpAreaPolicy.IsEligible(territory,Duty);
        // Native duty can still belong to the previous instance during a transfer.
        // Known PvP maps wait for a matching pair instead of caching that mismatch.
        if(!Eligible){Duty=0;return;}
        Pending=false;
    }
}
