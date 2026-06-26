using ElementalLoopTD.Core;
using ElementalLoopTD.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElementalLoopTD.UI;

public class BuildBar
{
    public string? ClickedType;

    public void Draw(SpriteBatch sb, SpriteFont font, GameManager gm, int screenWidth, int screenHeight, Rendering.TextureCache tex)
    {
        var barY = screenHeight - 50;
        sb.Draw(tex.BuildBarBg, new Vector2(0, barY), Color.White);

        var types = new[] { "fire", "water", "ice" };
        var btnW = 120;
        var totalW = types.Length * btnW + (types.Length - 1) * 8;
        var startX = (screenWidth - totalW) / 2;

        for (int i = 0; i < types.Length; i++)
        {
            var def = Config.Towers.All[types[i]];
            var built = gm.Towers.FirstOrDefault(t => t.Type == types[i]);
            var canAfford = built != null ? gm.Gold >= built.GetUpgradeCost() : gm.Gold >= def.Cost;
            var bx = startX + i * (btnW + 8);
            var btnRect = canAfford ? tex.BuildBarBtnNormal : tex.BuildBarBtnDim;
            sb.Draw(btnRect, new Vector2(bx, barY + 5), Color.White);
            sb.DrawString(font, def.Icon, new Vector2(bx + 10, barY + 8), def.Color);
            var label = built != null ? $"{def.Name} Lv{built.Level}" : def.Name;
            sb.DrawString(font, label, new Vector2(bx + 35, barY + 10), canAfford ? Color.White : Color.Gray);
            if (built != null)
                sb.DrawString(font, $"${built.GetUpgradeCost()}", new Vector2(bx + 35, barY + 25), new Color(255, 215, 0));
            else
                sb.DrawString(font, $"${def.Cost}", new Vector2(bx + 35, barY + 25), new Color(255, 215, 0));
        }
    }

    public bool HandleClick(int mx, int my, int screenWidth, int screenHeight)
    {
        var barY = screenHeight - 50;
        if (my < barY || my > barY + 50) return false;
        var types = new[] { "fire", "water", "ice" };
        var btnW = 120;
        var totalW = types.Length * btnW + (types.Length - 1) * 8;
        var startX = (screenWidth - totalW) / 2;
        for (int i = 0; i < types.Length; i++)
        {
            var bx = startX + i * (btnW + 8);
            if (mx >= bx && mx <= bx + btnW) { ClickedType = types[i]; return true; }
        }
        return false;
    }
}
