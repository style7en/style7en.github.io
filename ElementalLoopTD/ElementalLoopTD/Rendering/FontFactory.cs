using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElementalLoopTD.Rendering;

public static class FontFactory
{
    public static SpriteFont CreateDefaultSpriteFont(GraphicsDevice gd)
    {
        int charW = 9;
        int charH = 16;
        int first = 32;
        int last = 126;
        int count = last - first + 1;
        int texW = charW * count;
        int texH = charH;

        var tex = new Texture2D(gd, texW, texH);
        var data = new Color[texW * texH];
        for (int ci = 0; ci < count; ci++)
        {
            int cx = ci * charW;
            int ch = first + ci;
            bool isSpace = ch == ' ';
            for (int y = 0; y < charH; y++)
            {
                for (int x = 0; x < charW; x++)
                {
                    int px = cx + x;
                    int idx = y * texW + px;
                    if (isSpace)
                    {
                        data[idx] = Color.Transparent;
                    }
                    else
                    {
                        bool border = x == 0 || x == charW - 1 || y == 0 || y == charH - 1;
                        int innerX = x - 1;
                        int innerY = y - 1;
                        bool filled = false;
                        if (innerX >= 0 && innerX < charW - 2 && innerY >= 0 && innerY < charH - 2)
                        {
                            int rowPattern = ch switch
                            {
                                >= 'A' and <= 'Z' => ch - 'A',
                                >= 'a' and <= 'z' => ch - 'a' + 26,
                                >= '0' and <= '9' => ch - '0' + 52,
                                _ => (ch * 7 + 13) % 62
                            };
                            int bitIdx = innerX + innerY * (charW - 2);
                            filled = ((rowPattern * 7 + bitIdx * 3) & 1) == 1;
                        }
                        data[idx] = border || filled ? Color.White : Color.Transparent;
                    }
                }
            }
        }
        tex.SetData(data);

        var glyphBounds = new List<Rectangle>();
        var cropping = new List<Rectangle>();
        var characters = new List<char>();
        var kerning = new List<Vector3>();

        for (int ci = 0; ci < count; ci++)
        {
            char ch = (char)(first + ci);
            int cx = ci * charW;
            glyphBounds.Add(new Rectangle(cx, 0, charW, charH));
            cropping.Add(new Rectangle(0, 0, charW, charH));
            characters.Add(ch);
            kerning.Add(new Vector3(0, charW, 0));
        }

        return new SpriteFont(tex, glyphBounds, cropping, characters, charH + 2, 0, kerning, ' ');
    }
}