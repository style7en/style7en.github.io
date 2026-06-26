using ElementalLoopTD.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElementalLoopTD.Entities;

public class DamageParticle : Particle
{
    public int Dmg;
    public bool IsCrit;
    public string? TowerType;
    public float StartX;
    public float FloatSpeed;
    public float Scale;
    public float ShakePhase;

    public DamageParticle(float x, float y, int dmg, bool isCrit, string? towerType)
        : base(x + (float)(Random.Shared.NextDouble() - 0.5) * 10, y, "", GetColor(towerType, isCrit), isCrit ? 1.5f : 1.0f)
    {
        Dmg = dmg;
        IsCrit = isCrit;
        TowerType = towerType;
        StartX = Position.X;
        FloatSpeed = isCrit ? 45 : 30;
        Scale = isCrit ? 1.4f : 1.0f;
        ShakePhase = (float)(Random.Shared.NextDouble() * Math.PI * 2);
    }

    private static Color GetColor(string? towerType, bool isCrit)
    {
        return towerType switch
        {
            "fire" => new Color(255, 23, 68),
            "water" => new Color(61, 90, 254),
            "ice" => new Color(128, 222, 234),
            _ when isCrit => new Color(255, 235, 59),
            _ => Color.White,
        };
    }

    public override void Update(float dt)
    {
        Elapsed += dt;
        Position.Y -= FloatSpeed * dt;
        if (IsCrit)
            Position.X = StartX + MathF.Sin(ShakePhase + Elapsed * 12) * 4 * (1 - Elapsed / Duration);
        if (Elapsed >= Duration) Alive = false;
    }

    public override void Draw(SpriteBatch sb, SpriteFont font)
    {
        var alpha = Math.Max(0, 1 - Elapsed / Duration);
        var s = Scale;
        var text = IsCrit ? $"\u26a1{Dmg}" : $"{Dmg}";
        if (font != null)
        {
            var scale = new Vector2(s, s);
            var origin = font.MeasureString(text) / 2;
            sb.DrawString(font, text, Position, Color * alpha, 0, origin, scale, SpriteEffects.None, 0);
        }
    }
}