using Microsoft.Xna.Framework;

namespace ElementalLoopTD.Utils;

public static class SafeMath
{
    public static float Clamp(float v, float min, float max)
    {
        if (float.IsNaN(v) || float.IsInfinity(v)) return min;
        return Math.Clamp(v, min, max);
    }

    public static int SafeRound(float v)
    {
        if (!float.IsFinite(v)) return 0;
        return (int)Math.Round(v, MidpointRounding.AwayFromZero);
    }

    public static float SafeFinite(float v, float fallback = 0)
    {
        return float.IsFinite(v) ? v : fallback;
    }

    public static float SafeAdd(float a, float b, float cap)
    {
        var aa = SafeFinite(a, 0);
        var bb = SafeFinite(b, 0);
        var r = aa + bb;
        return Math.Clamp(r, -cap, cap);
    }

    public static float SafeMul(float a, float b, float cap)
    {
        var aa = SafeFinite(a, 0);
        var bb = SafeFinite(b, 0);
        var r = aa * bb;
        if (!float.IsFinite(r)) return 0;
        return Math.Clamp(r, -cap, cap);
    }

    public static int SafeNum(float v, int min, int max)
    {
        if (float.IsNaN(v) || float.IsInfinity(v)) return min;
        return Math.Clamp((int)v, min, max);
    }

    public static float Dist(float x1, float y1, float x2, float y2)
    {
        var dx = x1 - x2; var dy = y1 - y2;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    public static float DistSq(float x1, float y1, float x2, float y2)
    {
        var dx = x1 - x2; var dy = y1 - y2;
        return dx * dx + dy * dy;
    }

    public static float DistPointSeg(float px, float py, float ax, float ay, float bx, float by)
    {
        var dx = bx - ax; var dy = by - ay;
        var len2 = dx * dx + dy * dy;
        var t = len2 > 0 ? Math.Clamp(((px - ax) * dx + (py - ay) * dy) / len2, 0, 1) : 0f;
        var ex = px - (ax + t * dx);
        var ey = py - (ay + t * dy);
        return MathF.Sqrt(ex * ex + ey * ey);
    }
}
