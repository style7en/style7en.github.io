using Microsoft.Xna.Framework;

namespace ElementalLoopTD.Utils;

public static class ColorExtensions
{
    public static Color LerpColor(Color a, Color b, float t)
    {
        return new Color(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t),
            (byte)(a.A + (b.A - a.A) * t)
        );
    }

    public static Color HpColor(float ratio)
    {
        if (!float.IsFinite(ratio)) ratio = 1;
        ratio = Math.Clamp(ratio, 0, 1);
        var green = new Color(76, 175, 80);
        var yellow = new Color(255, 235, 59);
        var red = new Color(244, 67, 54);
        if (ratio > 0.5f)
            return LerpColor(yellow, green, (ratio - 0.5f) * 2);
        else
            return LerpColor(red, yellow, ratio * 2);
    }
}
