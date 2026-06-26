using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElementalLoopTD.Rendering;

public class GrassRenderer
{
    private RenderTarget2D? _cache;
    private int _cacheW, _cacheH;

    public void BuildCache(GraphicsDevice gd, int mapW, int mapH, int seed)
    {
        if (_cache != null && _cacheW == mapW && _cacheH == mapH) return;
        _cacheW = mapW; _cacheH = mapH;
        _cache?.Dispose();
        _cache = new RenderTarget2D(gd, Math.Max(1, mapW), Math.Max(1, mapH));
        gd.SetRenderTarget(_cache);
        gd.Clear(new Color(45, 74, 31));

        var sb = new SpriteBatch(gd);
        sb.Begin();

        var rng = new Random(seed);
        var blades = Math.Max(1, (mapW * mapH) / 240);
        var bladeColors = new[] {
            new Color(60, 110, 50),
            new Color(80, 140, 70),
            new Color(70, 120, 60)
        };

        for (int i = 0; i < blades; i++)
        {
            var x = rng.Next(Math.Max(1, mapW));
            var y = rng.Next(Math.Max(1, mapH));
            var len = 3 + rng.Next(4);
            var color = bladeColors[rng.Next(3)] * (0.55f + (float)rng.NextDouble() * 0.3f);
            var pixel = TextureGenerator.CreateRect(gd, 1, 1, color);
            var angle = -MathF.PI / 2 + (float)(rng.NextDouble() - 0.5) * 0.5f;
            sb.Draw(pixel, new Vector2(x, y), null, color, angle,
                new Vector2(0, 0), new Vector2(1, len), SpriteEffects.None, 0);
        }

        sb.End();
        gd.SetRenderTarget(null);
    }

    public void Draw(SpriteBatch sb, int offsetX, int offsetY)
    {
        if (_cache != null)
            sb.Draw(_cache, new Vector2(offsetX, offsetY), Color.White);
    }
}