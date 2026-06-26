using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElementalLoopTD.Rendering;

public class TextureCache
{
    public Texture2D Pixel { get; private set; } = null!;
    public Texture2D BorderPixel { get; private set; } = null!;
    public Texture2D HudBg { get; private set; } = null!;
    public Texture2D BuildBarBg { get; private set; } = null!;
    public Texture2D PanelBg { get; private set; } = null!;
    public Texture2D HpBarBg { get; private set; } = null!;
    public Texture2D HpBarDark { get; private set; } = null!;
    public Texture2D HpBarGreen { get; private set; } = null!;
    public Texture2D HpBarYellow { get; private set; } = null!;
    public Texture2D HpBarRed { get; private set; } = null!;
    public Texture2D OverlayPause { get; private set; } = null!;
    public Texture2D OverlayGameOver { get; private set; } = null!;
    public Texture2D OverlayRestore { get; private set; } = null!;
    public Texture2D BuildBarBtnNormal { get; private set; } = null!;
    public Texture2D BuildBarBtnDim { get; private set; } = null!;
    public Texture2D RangeCircle { get; private set; } = null!;

    public Dictionary<string, Texture2D> TowerCircles { get; } = new();
    public Dictionary<int, Texture2D> MonsterCircles { get; } = new();

    public void Build(GraphicsDevice gd, int screenW, int screenH)
    {
        Pixel = TextureGenerator.CreateRect(gd, 1, 1, Color.White);
        BorderPixel = TextureGenerator.CreateRect(gd, 1, 1, new Color(10, 26, 5));

        var towerColors = new Dictionary<string, Color>
        {
            ["fire"] = new Color(255, 87, 34),
            ["water"] = new Color(33, 150, 243),
            ["ice"] = new Color(0, 188, 212),
        };
        foreach (var (type, color) in towerColors)
            TowerCircles[type] = TextureGenerator.CreateCircle(gd, 26, color);

        for (int r = 8; r <= 20; r++)
            MonsterCircles[r] = TextureGenerator.CreateCircle(gd, r, Color.White);

        RangeCircle = TextureGenerator.CreateCircle(gd, 200, Color.White);

        HudBg = TextureGenerator.CreateRect(gd, screenW, 30, new Color(22, 33, 62));
        BuildBarBg = TextureGenerator.CreateRect(gd, screenW, 50, new Color(22, 33, 62));
        PanelBg = TextureGenerator.CreateRect(gd, 220, 100, new Color(15, 52, 96) * 0.96f);
        OverlayPause = TextureGenerator.CreateRect(gd, screenW, screenH, new Color(0, 0, 0, 140));
        OverlayGameOver = TextureGenerator.CreateRect(gd, screenW, screenH, new Color(0, 0, 0, 191));
        OverlayRestore = TextureGenerator.CreateRect(gd, screenW, screenH, new Color(0, 0, 0, 204));

        BuildBarBtnNormal = TextureGenerator.CreateRect(gd, 120, 40, new Color(26, 26, 46));
        BuildBarBtnDim = TextureGenerator.CreateRect(gd, 120, 40, new Color(26, 26, 46) * 0.4f);

        HpBarBg = TextureGenerator.CreateRect(gd, 28, 6, new Color(0, 0, 0, 140));
        HpBarDark = TextureGenerator.CreateRect(gd, 26, 4, new Color(51, 51, 51));
        HpBarGreen = TextureGenerator.CreateRect(gd, 26, 4, new Color(76, 175, 80));
        HpBarYellow = TextureGenerator.CreateRect(gd, 26, 4, new Color(255, 235, 59));
        HpBarRed = TextureGenerator.CreateRect(gd, 26, 4, new Color(244, 67, 54));
    }

    public void RebuildScreenTextures(GraphicsDevice gd, int screenW, int screenH)
    {
        HudBg?.Dispose();
        BuildBarBg?.Dispose();
        OverlayPause?.Dispose();
        OverlayGameOver?.Dispose();
        OverlayRestore?.Dispose();
        HudBg = TextureGenerator.CreateRect(gd, screenW, 30, new Color(22, 33, 62));
        BuildBarBg = TextureGenerator.CreateRect(gd, screenW, 50, new Color(22, 33, 62));
        OverlayPause = TextureGenerator.CreateRect(gd, screenW, screenH, new Color(0, 0, 0, 140));
        OverlayGameOver = TextureGenerator.CreateRect(gd, screenW, screenH, new Color(0, 0, 0, 191));
        OverlayRestore = TextureGenerator.CreateRect(gd, screenW, screenH, new Color(0, 0, 0, 204));
    }
}
