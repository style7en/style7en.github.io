using ElementalLoopTD.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElementalLoopTD.Rendering;

public static class TextureGenerator
{
    public static Texture2D CreateCircle(GraphicsDevice gd, int radius, Color color, bool filled = true)
    {
        var size = radius * 2;
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

    public static Texture2D CreateRect(GraphicsDevice gd, int w, int h, Color color)
    {
        var tex = new Texture2D(gd, Math.Max(1, w), Math.Max(1, h));
        var data = new Color[Math.Max(1, w) * Math.Max(1, h)];
        Array.Fill(data, color);
        tex.SetData(data);
        return tex;
    }

    public static Texture2D CreateProjectileTexture(GraphicsDevice gd, bool isCrit)
    {
        var r = isCrit ? 5 : 3;
        var col = isCrit ? new Color(255, 235, 59) : Color.White;
        return CreateCircle(gd, r, col);
    }

    public static Texture2D CreateTowerTexture(GraphicsDevice gd, TowerDef def, int level, float size)
    {
        var s = (int)(size * 2);
        var rt = new RenderTarget2D(gd, s, s);
        gd.SetRenderTarget(rt);
        gd.Clear(Color.Transparent);

        var sb = new SpriteBatch(gd);
        sb.Begin();

        var bodyW = s * 0.35f; var bodyH = s * 0.35f;
        var bx = (s - bodyW) / 2; var by = (s - bodyH) / 2 + 5;
        var bodyTex = CreateRect(gd, (int)bodyW, (int)bodyH, def.Color);
        sb.Draw(bodyTex, new Vector2(bx, by), Color.White);

        var accentTex = CreateRect(gd, (int)(bodyW * 0.8f), 2, Color.White * 0.35f);
        sb.Draw(accentTex, new Vector2(bx + bodyW * 0.1f, by), Color.White);

        sb.End();

        gd.SetRenderTarget(null);
        return rt;
    }
}