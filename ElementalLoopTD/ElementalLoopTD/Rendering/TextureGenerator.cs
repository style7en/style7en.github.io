using ElementalLoopTD.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElementalLoopTD.Rendering;

public static class TextureGenerator
{
    public static Texture2D CreateCircle(GraphicsDevice gd, int radius, Color color, bool filled = true)
    {
        var size = Math.Max(1, radius * 2);
        var tex = new Texture2D(gd, size, size);
        var data = new Color[size * size];
        var rSq = radius * radius;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            var dx = x - radius; var dy = y - radius;
            var distSq = dx * dx + dy * dy;
            data[y * size + x] = distSq <= rSq ? color : Color.Transparent;
        }
        tex.SetData(data);
        return tex;
    }

    public static Texture2D CreateCircleOutline(GraphicsDevice gd, int radius, Color color, int lineWidth = 1)
    {
        var size = Math.Max(1, radius * 2);
        var tex = new Texture2D(gd, size, size);
        var data = new Color[size * size];
        var rSq = radius * radius;
        var innerSq = (radius - lineWidth) * (radius - lineWidth);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            var dx = x - radius; var dy = y - radius;
            var distSq = dx * dx + dy * dy;
            data[y * size + x] = distSq <= rSq && distSq > innerSq ? color : Color.Transparent;
        }
        tex.SetData(data);
        return tex;
    }

    public static Texture2D CreateRect(GraphicsDevice gd, int w, int h, Color color)
    {
        w = Math.Max(1, w); h = Math.Max(1, h);
        var tex = new Texture2D(gd, w, h);
        var data = new Color[w * h];
        Array.Fill(data, color);
        tex.SetData(data);
        return tex;
    }

    // Round rect texture for tower base
    public static Texture2D CreateRoundRect(GraphicsDevice gd, int w, int h, Color color)
    {
        var tex = new Texture2D(gd, w, h);
        var data = new Color[w * h];
        var r = Math.Min(3, Math.Min(w, h) / 2);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            // Simple rounded corner check
            bool corner = false;
            if (x < r && y < r) corner = (x - r) * (x - r) + (y - r) * (y - r) > r * r;
            else if (x >= w - r && y < r) corner = (x - (w - r - 1)) * (x - (w - r - 1)) + (y - r) * (y - r) > r * r;
            else if (x < r && y >= h - r) corner = (x - r) * (x - r) + (y - (h - r - 1)) * (y - (h - r - 1)) > r * r;
            else if (x >= w - r && y >= h - r) corner = (x - (w - r - 1)) * (x - (w - r - 1)) + (y - (h - r - 1)) * (y - (h - r - 1)) > r * r;
            data[y * w + x] = corner ? Color.Transparent : color;
        }
        tex.SetData(data);
        return tex;
    }
}