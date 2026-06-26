using ElementalLoopTD.Core;
using ElementalLoopTD.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElementalLoopTD.UI;

public class InfoPanel
{
    private int _lastSig;
    private string _cachedHtml = "";

    public void Draw(SpriteBatch sb, SpriteFont font, GameManager gm, int screenWidth, int screenHeight)
    {
        if (gm.SelectedTower == null) return;
        var t = gm.SelectedTower;
        var sig = t.Type.GetHashCode() ^ t.Level.GetHashCode();
        if (sig != _lastSig)
        {
            _cachedHtml = $"{t.Def.Icon} {t.Def.Name} Lv.{t.Level}{(t.IsUltimate() ? " ★" : "")}\n" +
                          $"攻击 {t.GetAtk()}  范围 {t.GetRangeRatio()*100:F1}%\n" +
                          $"攻速 {t.GetSpeed():F1}/s  暴击 {t.CritRate*100:F0}%  爆伤 {t.CritDamage*100:F0}%\n" +
                          $"升级 ${t.GetUpgradeCost()}";
            _lastSig = sig;
        }
        var pos = t.Position + new Vector2(30, -40);
        if (pos.X + 200 > screenWidth) pos.X = t.Position.X - 230;
        if (pos.Y < 40) pos.Y = 40;
        if (pos.Y + 100 > screenHeight) pos.Y = screenHeight - 110;

        var panelTex = TextureGenerator.CreateRect(sb.GraphicsDevice, 220, 100, new Color(15, 52, 96) * 0.96f);
        sb.Draw(panelTex, pos, Color.White);
        sb.DrawString(font, _cachedHtml, pos + new Vector2(5, 5), Color.White);
    }
}
