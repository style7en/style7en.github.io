using ElementalLoopTD.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElementalLoopTD.Rendering;

public class PathRenderer
{
    private RenderTarget2D? _cache;
    private int _cacheW, _cacheH;

    public void BuildCache(GraphicsDevice gd, List<Waypoint> waypoints, int width, int height)
    {
        if (_cache != null && _cacheW == width && _cacheH == height) return;
        _cacheW = width; _cacheH = height;
        _cache?.Dispose();
        _cache = new RenderTarget2D(gd, width, height);
        gd.SetRenderTarget(_cache);
        gd.Clear(Color.Transparent);
        var sb = new SpriteBatch(gd);
        sb.Begin();

        if (waypoints.Count >= 6)
        {
            var polyline = new[] { waypoints[0], waypoints[1], waypoints[2], waypoints[3], waypoints[4], waypoints[5], waypoints[1] };
            for (int i = 0; i < polyline.Length - 1; i++)
            {
                var a = polyline[i]; var b = polyline[i + 1];
                DrawThickLine(sb, gd, a.X, a.Y, b.X, b.Y, 22, new Color(44, 40, 32));
            }
            var stoneColor = new Color(165, 163, 153);
            for (int i = 0; i < polyline.Length - 1; i++)
            {
                var a = polyline[i]; var b = polyline[i + 1];
                var len = Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
                var steps = (int)(len / 22);
                for (int j = 0; j <= steps; j++)
                {
                    var t = j / (float)steps;
                    var sx = (float)(a.X + (b.X - a.X) * t);
                    var sy = (float)(a.Y + (b.Y - a.Y) * t);
                    var stoneTex = TextureGenerator.CreateCircle(gd, 8, stoneColor);
                    sb.Draw(stoneTex, new Vector2(sx - 8, sy - 8), Color.White);
                }
            }
        }

        sb.End();
        gd.SetRenderTarget(null);
    }

    private void DrawThickLine(SpriteBatch sb, GraphicsDevice gd, float x1, float y1, float x2, float y2, float thickness, Color color)
    {
        var dx = x2 - x1; var dy = y2 - y1;
        var len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 1) return;
        var angle = MathF.Atan2(dy, dx);
        var pixel = TextureGenerator.CreateRect(gd, 1, 1, color);
        sb.Draw(pixel, new Vector2(x1, y1), null, color, angle,
            new Vector2(0, 0.5f), new Vector2(len, thickness), SpriteEffects.None, 0);
    }

    public void Draw(SpriteBatch sb)
    {
        if (_cache != null) sb.Draw(_cache, Vector2.Zero, Color.White);
    }
}