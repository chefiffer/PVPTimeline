namespace PvpTimeline;

/// <summary>Keep mount-sheet IDs out of the battle-action namespace.</summary>
internal static class MountObservation
{
    // Local display identity, never passed to the game. All mount types share it.
    public const uint ActionId=0xFFFF0001;
    public const uint Icon=118; // GeneralAction 9: the game's generic Mount Roulette icon.
    public static uint CastAction(uint type,uint id)=>type switch {13=>ActionId,1=>id,_=>0};
    // The supplied trace reports SpellId=4 and Mount effect 39 (value=mount ID).
    // Do not treat a real battle action numbered 4 or 241 as a mount by ID alone.
    public static bool IsMountResult(uint type,uint spell,byte effect)=>type==13 || spell==4 && effect==39;
    public static void ReceiveMount(TimelineModel model,ActionInfo info,double time,ulong target,ushort sequence,uint global,float animationLock)
    {
        var entry=model.Acknowledged(info,time,target,sequence,global,animationLock);
        entry.HasDirectResponse=true; // Successful mounting, not a damage result.
        model.Effect(info,time,(uint)target,39,0,sequence,global);
    }
}
