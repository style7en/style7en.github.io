using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace ElementalLoopTD.Rendering;

public static class FontFactory
{
    // Characters we need: ASCII printable + Chinese chars used in game
    private static readonly string Chars = " 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz" +
        "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~" +
        "波数金币怪物击杀升级攻击范围攻速暴击爆伤觉醒蒸发冻结融化元素循圈塔防存发现已座按恢复进度新游戏继续暂停";

    public static SpriteFont? Create(GraphicsDevice gd)
    {
        try
        {
            int fontSize = 16;
            using var bmp = new Bitmap(1, 1);
            using var g = Graphics.FromImage(bmp);
            var font = new System.Drawing.Font("Microsoft YaHei", fontSize, GraphicsUnit.Pixel);

            // Measure each character
            var charWidths = new Dictionary<char, int>();
            var charHeight = 0;
            foreach (char ch in Chars.Distinct())
            {
                var size = g.MeasureString(ch.ToString(), font);
                charWidths[ch] = (int)Math.Ceiling(size.Width) + 1;
                charHeight = Math.Max(charHeight, (int)Math.Ceiling(size.Height));
            }
            charHeight += 2;

            // Build texture atlas
            int spacing = 1;
            int totalWidth = charWidths.Values.Sum() + spacing * charWidths.Count;
            using var atlas = new Bitmap(totalWidth, charHeight);
            using var atlasG = Graphics.FromImage(atlas);
            atlasG.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            atlasG.Clear(System.Drawing.Color.Transparent);

            var glyphBounds = new List<Rectangle>();
            var cropping = new List<Rectangle>();
            var characters = new List<char>();
            var kerning = new List<Vector3>();

            int cx = 0;
            foreach (char ch in Chars.Distinct())
            {
                int w = charWidths[ch];
                atlasG.DrawString(ch.ToString(), font, System.Drawing.Brushes.White, cx, 0);
                glyphBounds.Add(new Rectangle(cx, 0, w, charHeight));
                cropping.Add(new Rectangle(0, 0, w, charHeight));
                characters.Add(ch);
                kerning.Add(new Vector3(0, w, 0));
                cx += w + spacing;
            }

            // Copy bitmap data to Texture2D
            var data = new Color[totalWidth * charHeight];
            var bd = atlas.LockBits(new System.Drawing.Rectangle(0, 0, totalWidth, charHeight),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var ptr = bd.Scan0;
            var bytes = new byte[totalWidth * charHeight * 4];
            System.Runtime.InteropServices.Marshal.Copy(ptr, bytes, 0, bytes.Length);
            atlas.UnlockBits(bd);

            for (int y = 0; y < charHeight; y++)
            for (int x = 0; x < totalWidth; x++)
            {
                int i = (y * totalWidth + x) * 4;
                byte a = bytes[i + 3];
                data[y * totalWidth + x] = new Color(bytes[i + 2], bytes[i + 1], bytes[i + 0], a);
            }

            var tex = new Texture2D(gd, totalWidth, charHeight);
            tex.SetData(data);

            return new SpriteFont(tex, glyphBounds, cropping, characters, charHeight + 2, 0, kerning, ' ');
        }
        catch
        {
            return null;
        }
    }
}