using System.Numerics;
using Dalamud.Configuration;

namespace PvpTimeline;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 3;
    public bool Enabled = true;
    public int Language = -1;
    public float UiScale=1;
    public Vector4 LockedBackground=new(0,0,0,.35f);
    public Vector4 UnlockedBackground=new(0,0,0,.8f);
    public bool Locked;
    public bool CombatOnly;
    public bool HideInCutscene = true;
    public bool Horizontal = true;
    public bool Reverse;
    public bool ShowAbilities = true;
    public bool ShowAutoAttack;
    public bool ShowCast = true;
    public bool ShowGcd = true;
    public bool ShowLock = true;
    public bool ShowAnimation = true;
    public bool ObserveNativeAnimations = true;
    public bool ShowEffects = true;
    public bool ShowCooldown;
    public bool ShowOwnBuffs = true;
    public int BuffRows = 2;
    public float BuffRowHeight = 18;
    public bool ShowRecuperateCooldown = true;
    public float TriggeredIconOpacity = .4f;
    public bool ShowGrid = true;
    public bool ShowSeconds = true;
    public bool ShowLegend;
    public bool ShowUnconfirmed = true;
    public float PixelsPerSecond = 60;
    public float HistorySeconds = 15;
    public float FutureSeconds = 2;
    public float GcdIconSize = 44;
    public float AbilityIconSize = 28;
    public float RowGap = 12;
    public float LineThickness = 6;
    public float AbilityLineThickness = 6;
    public float AbilityVerticalOffset = -30;
    public float GcdBarOffset = 0;
    public float AbilityBarOffset = 0;
    public float AnimationVerticalOffset = 12;
    public float CastVerticalOffset = -8;
    public float EffectMarkerSize = 4;
    public bool HighlightDamage = true;
    public float IconInset = 4;
    public float AbilityLaneGap = 6;
    public float BarFillOpacity = .25f;
    public int Subdivisions = 2;
    public float LockedOpacity = .35f;
    public float UnlockedOpacity = .8f;
    public Vector4 CastColor = new(.35f, .9f, .4f, 1);
    public Vector4 GcdColor = new(.4f, .65f, 1, 1);
    public Vector4 LockColor = new(1, .92f, 0, 1);
    public Vector4 AnimationColor = new(.8f, .5f, 1, 1);
    public Vector4 EffectColor = new(1, .35f, .4f, 1);
    public Vector4 CooldownColor = new(.65f, .7f, .75f, .7f);
    public Vector4 CriticalColor = new(1, .5f, 0, 1);
    public Vector4 DirectColor = new(1, .9f, 0, 1);
    public Vector4 CriticalDirectColor = new(1, .2f, .2f, 1);
    public Vector4 AnimationReplacedColor = new(1, .6f, .1f, 1);
    public void Normalize()
    {
        if(LockColor==new Vector4(1,.7f,.3f,1))LockColor=new(1,.92f,0,1);
        static float Clamp(float v, float fallback, float min, float max) => float.IsFinite(v) ? Math.Clamp(v, min, max) : fallback;
        Language=Math.Clamp(Language,0,5);UiScale=Clamp(UiScale,1,.2f,2);
        // Persisted color values are untrusted input; reject NaN/Infinity before passing them to ImGui.
        static Vector4 SafeColor(Vector4 c)=>new(float.IsFinite(c.X)?Math.Clamp(c.X,0,1):0,float.IsFinite(c.Y)?Math.Clamp(c.Y,0,1):0,float.IsFinite(c.Z)?Math.Clamp(c.Z,0,1):0,float.IsFinite(c.W)?Math.Clamp(c.W,0,1):1);
        LockedBackground=SafeColor(LockedBackground);UnlockedBackground=SafeColor(UnlockedBackground);
        CastColor=SafeColor(CastColor);GcdColor=SafeColor(GcdColor);LockColor=SafeColor(LockColor);AnimationColor=SafeColor(AnimationColor);
        EffectColor=SafeColor(EffectColor);CooldownColor=SafeColor(CooldownColor);CriticalColor=SafeColor(CriticalColor);DirectColor=SafeColor(DirectColor);CriticalDirectColor=SafeColor(CriticalDirectColor);AnimationReplacedColor=SafeColor(AnimationReplacedColor);
        PixelsPerSecond = Clamp(PixelsPerSecond, 60, 20, 150);
        BuffRows=Math.Clamp(BuffRows,1,2);
        BuffRowHeight=Clamp(BuffRowHeight,18,14,28);
        HistorySeconds = Clamp(HistorySeconds, 15, 5, 60);
        FutureSeconds = Clamp(FutureSeconds, 2, 0, 10);
        GcdIconSize = Clamp(GcdIconSize, 44, 24, 80);
        AbilityIconSize = Clamp(AbilityIconSize, 28, 16, GcdIconSize);
        RowGap = Clamp(RowGap, 12, 4, 40);
        LineThickness = Clamp(LineThickness, 6, 1, 12);
        AbilityLineThickness = Clamp(AbilityLineThickness, 6, 1, 12);
        AbilityVerticalOffset = Clamp(AbilityVerticalOffset, -30, -100, 40);
        GcdBarOffset = Clamp(GcdBarOffset, 0, -40, 60);
        AbilityBarOffset = Clamp(AbilityBarOffset, 0, -40, 60);
        AnimationVerticalOffset = Clamp(AnimationVerticalOffset, 12, 2, 80);
        CastVerticalOffset = Clamp(CastVerticalOffset, -8, -40, 40);
        EffectMarkerSize = Clamp(EffectMarkerSize, 4, 2, 10);
        IconInset = Clamp(IconInset, 4, 0, 20);
        AbilityLaneGap = Clamp(AbilityLaneGap, 6, 0, 24);
        BarFillOpacity = Clamp(BarFillOpacity, .25f, .05f, .8f);
        TriggeredIconOpacity = Clamp(TriggeredIconOpacity,.4f,.15f,.75f);
        Subdivisions = Math.Clamp(Subdivisions, 1, 8);
        LockedOpacity = Clamp(LockedOpacity, .35f, 0, 1);
        UnlockedOpacity = Clamp(UnlockedOpacity, .8f, 0, 1);
    }
}
