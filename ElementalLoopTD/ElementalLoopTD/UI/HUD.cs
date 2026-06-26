using ElementalLoopTD.Core;
using ElementalLoopTD.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElementalLoopTD.UI;

public class HUD
{
    private string _waveText = "", _goldText = "", _monsterText = "", _killText = "";
    private int _lastWave = -1, _lastGold = -1, _lastKills = -1, _lastAlive = -1;

    public void Draw(SpriteBatch sb, SpriteFont font, GameManager gm, int screenWidth, Rendering.TextureCache tex)
    {
        if (gm.Wave != _lastWave) { _waveText = $"波数: {gm.Wave}"; _lastWave = gm.Wave; }
        if (gm.Gold != _lastGold) { _goldText = $"金币: {gm.Gold}"; _lastGold = gm.Gold; }
        if (gm.Kills != _lastKills) { _killText = $"击杀: {gm.Kills}"; _lastKills = gm.Kills; }
        var alive = gm.Monsters.Count(m => m.Alive);
        if (alive != _lastAlive) { _monsterText = $"怪物: {alive}/{Config.Combat.MaxMonstersGameover}"; _lastAlive = alive; }

        sb.Draw(tex.HudBg, Vector2.Zero, Color.White);

        var x = 10f;
        sb.DrawString(font, _waveText, new Vector2(x, 5), new Color(168, 168, 179));
        x += font.MeasureString(_waveText).X + 15;
        sb.DrawString(font, _goldText, new Vector2(x, 5), new Color(255, 215, 0));
        x += font.MeasureString(_goldText).X + 15;
        sb.DrawString(font, _monsterText, new Vector2(x, 5), new Color(255, 76, 76));
        x += font.MeasureString(_monsterText).X + 15;
        sb.DrawString(font, _killText, new Vector2(x, 5), Color.White);
    }
}
