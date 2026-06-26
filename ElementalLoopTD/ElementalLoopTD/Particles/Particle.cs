using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElementalLoopTD.Entities;

public class Particle
{
    public Vector2 Position;
    public string Text;
    public Color Color;
    public float Duration;
    public float Elapsed;
    public bool Alive = true;

    public Particle(float x, float y, string text, Color color, float duration = 1.5f)
    {
        Position = new Vector2(x, y);
        Text = text;
        Color = color;
        Duration = duration;
    }

    public virtual void Update(float dt)
    {
        Elapsed += dt;
        Position.Y -= 30 * dt;
        if (Elapsed >= Duration) Alive = false;
    }

    public virtual void Draw(SpriteBatch sb, SpriteFont font)
    {
        var alpha = Math.Max(0, 1 - Elapsed / Duration);
        if (font != null)
            sb.DrawString(font, Text, Position, Color * alpha);
    }
}