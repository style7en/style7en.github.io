using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElementalLoopTD.Entities;

public class MergeBurst : Particle
{
    public float R0 = 14, R1 = 60;

    public MergeBurst(float x, float y, Color color)
        : base(x, y, "", color, 0.55f) { }

    public override void Update(float dt)
    {
        Elapsed += dt;
        if (Elapsed >= Duration) Alive = false;
    }

    public override void Draw(SpriteBatch sb, SpriteFont font)
    {
        // Ring rendering handled in main draw loop
    }
}