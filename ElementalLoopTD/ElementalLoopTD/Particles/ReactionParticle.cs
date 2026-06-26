using ElementalLoopTD.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElementalLoopTD.Entities;

public class ReactionParticle : Particle
{
    public string Label;
    public string Icon;
    public float PulsePhase;

    public ReactionParticle(float x, float y, string label, ElementVisual visual)
        : base(x, y, "", visual.Color, 1.6f)
    {
        Label = label;
        Icon = visual.Icon;
        PulsePhase = (float)(Random.Shared.NextDouble() * Math.PI * 2);
    }

    public override void Draw(SpriteBatch sb, SpriteFont font)
    {
        var alpha = Math.Max(0, 1 - Elapsed / Duration);
        var s = 1f + 0.25f * MathF.Sin(PulsePhase + Elapsed * 8);
        if (font != null)
        {
            sb.DrawString(font, Icon, Position, Color * alpha, 0, Vector2.Zero, s, SpriteEffects.None, 0);
            sb.DrawString(font, Label, Position + new Vector2(0, 18), Color * alpha, 0, Vector2.Zero, s * 0.7f, SpriteEffects.None, 0);
        }
    }
}