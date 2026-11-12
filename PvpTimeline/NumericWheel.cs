namespace PvpTimeline;

/// <summary>Pure wheel arithmetic in canonical units; shared by integer/percentage/scale editors.</summary>
internal static class NumericWheel
{
    public static float Apply(float value,float wheel,float step,float min,float max)
    {
        if(!float.IsFinite(value))value=min;
        if(!float.IsFinite(wheel) || !float.IsFinite(step) || step<=0)return Math.Clamp(value,min,max);
        return Math.Clamp(value+wheel*step,min,max);
    }
}
