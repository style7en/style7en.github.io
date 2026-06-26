using ElementalLoopTD.Core;
using ElementalLoopTD.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElementalLoopTD.UI;

public class Overlays
{
    public void DrawPause(SpriteBatch sb, SpriteFont font, int screenWidth, int screenHeight)
    {
        var overlay = TextureGenerator.CreateRect(sb.GraphicsDevice, screenWidth, screenHeight, new Color(0, 0, 0, 140));
        sb.Draw(overlay, Vector2.Zero, Color.White);
        var text = "⏸ 已暂停\n\n按 P / 空格继续";
        var size = font.MeasureString(text);
        var pos = new Vector2((screenWidth - size.X) / 2, (screenHeight - size.Y) / 2);
        sb.DrawString(font, text, pos, new Color(76, 175, 80));
    }

    public void DrawGameOver(SpriteBatch sb, SpriteFont font, GameManager gm, int screenWidth, int screenHeight)
    {
        var overlay = TextureGenerator.CreateRect(sb.GraphicsDevice, screenWidth, screenHeight, new Color(0, 0, 0, 191));
        sb.Draw(overlay, Vector2.Zero, Color.White);
        var text = $"Game Over\n\n存活波数: {gm.Wave}\n总击杀数: {gm.Kills}";
        var size = font.MeasureString(text);
        var pos = new Vector2((screenWidth - size.X) / 2, (screenHeight - size.Y) / 2);
        sb.DrawString(font, text, pos, new Color(233, 69, 96));
    }
}
