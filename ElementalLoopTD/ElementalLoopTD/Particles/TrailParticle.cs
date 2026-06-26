using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElementalLoopTD.Entities;

public class TrailParticle : Particle
{
    public Vector2 Start, End;
    public Color TrailColor;

    public TrailParticle(float x0, float y0, float x1, float y1, Color color)
        : base(x0, y0, "", color, 0.45f)
    {
        Start = new Vector2(x0, y0);
        End = new Vector2(x1, y1);
        TrailColor = color;
    }

    public override void Update(float dt)
    {
        Elapsed += dt;
        if (Elapsed >= Duration) Alive = false;
    }

    public override void Draw(SpriteBatch sb, SpriteFont font)
    {
        // Trail rendering handled in main draw loop as line segments
    }
}