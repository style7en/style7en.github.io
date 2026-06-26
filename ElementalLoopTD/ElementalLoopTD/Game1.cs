using ElementalLoopTD.Core;
using ElementalLoopTD.Entities;
using ElementalLoopTD.Rendering;
using ElementalLoopTD.UI;
using ElementalLoopTD.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ElementalLoopTD;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private SpriteFont _font = null!;
    private readonly TextureCache _texCache = new();

    private readonly GameManager _gm = new();
    private readonly HUD _hud = new();
    private readonly InfoPanel _infoPanel = new();
    private readonly BuildBar _buildBar = new();
    private readonly Overlays _overlays = new();
    private readonly PathRenderer _pathRenderer = new();
    private readonly GrassRenderer _grassRenderer = new();

    private int _windowWidth = 800, _windowHeight = 600;
    private float _mapLeft, _mapTop, _mapRight, _mapBottom, _mapW;
    private bool _savePending;
    private float _saveTimer;
    private bool _showRestoreDialog;
    private string _restoreInfo = "";

    private float _fpsAcc;
    private int _fpsFrames, _fps;

    private ButtonState _prevLeft = ButtonState.Released;
    private ButtonState _prevP = ButtonState.Released;
    private ButtonState _prevSpace = ButtonState.Released;
    private ButtonState _prevY = ButtonState.Released;
    private ButtonState _prevN = ButtonState.Released;
    private ButtonState _prevEscape = ButtonState.Released;

    private int _mouseX, _mouseY;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = _windowWidth;
        _graphics.PreferredBackBufferHeight = _windowHeight;
        Content.RootDirectory = "Content";
        Window.Title = "元素循环圈塔防";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
    }

    protected override void Initialize()
    {
        Window.ClientSizeChanged += OnResize;
        OnResize(null, EventArgs.Empty);
        base.Initialize();
    }

    private void OnResize(object? sender, EventArgs e)
    {
        _windowWidth = GraphicsDevice.Viewport.Width;
        _windowHeight = GraphicsDevice.Viewport.Height;
        if (_texCache != null)
            _texCache.RebuildScreenTextures(GraphicsDevice, _windowWidth, _windowHeight);
        RebuildMap();
    }

    private void RebuildMap()
    {
        var padding = (int)(Math.Min(_windowWidth, _windowHeight) * 0.06f);
        _mapLeft = padding;
        _mapTop = 40;
        _mapRight = _windowWidth - padding;
        _mapBottom = _windowHeight - 50 - padding;
        _mapW = _mapRight - _mapLeft;
        if (_mapW < 100) return;
        _gm.BuildWaypoints(_windowWidth, _windowHeight, _mapLeft, _mapTop, _mapRight, _mapBottom);
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _font = FontFactory.Create(GraphicsDevice);

        _gm.OnStateChanged += () => ScheduleSave();
        _gm.OnRuleHint += (msg) => { };
        _gm.OnWaveNotice += (msg) => { };

        _texCache.Build(GraphicsDevice, _windowWidth, _windowHeight);
        RebuildMap();
        _pathRenderer.BuildCache(GraphicsDevice, _gm.Waypoints, _windowWidth, _windowHeight);
        _grassRenderer.BuildCache(GraphicsDevice, (int)_mapW, (int)(_mapW), 42);

        CheckForSave();
    }

    private void CheckForSave()
    {
        var data = SaveManager.Load();
        if (data != null)
        {
            _showRestoreDialog = true;
            _restoreInfo = $"发现存档: 波数{data.State.Wave}, 金币${data.State.Gold}, 击杀{data.State.Kills}, 塔{data.State.Towers.Count}座";
        }
    }

    private void RestoreFromSave(SaveData data)
    {
        var s = data.State;
        _gm.Gold = Math.Clamp(s.Gold, 0, int.MaxValue);
        _gm.Wave = Math.Clamp(s.Wave, 0, Config.Combat.MaxSafeWave);
        _gm.Kills = Math.Max(0, s.Kills);
        _gm.Towers.Clear();
        foreach (var td in s.Towers)
        {
            if (!Config.Towers.All.ContainsKey(td.Type)) continue;
            var t = new Tower(td.Type, td.X, td.Y);
            t.Level = Math.Max(1, td.Level);
            t.CritRate = Math.Clamp(td.CritRate, 0, Config.Combat.MaxCritRate);
            t.CritDamage = Math.Clamp(td.CritDamage, 1.5f, Config.Combat.MaxCritDmg);
            t.BonusRangeRatio = Math.Clamp(td.BonusRangeRatio, 0, Config.RangeRatioMax);
            t.BonusSpeed = Math.Clamp(td.BonusSpeed, 0, Config.Combat.MaxAtkSpeed - t.Def.BaseSpeed);
            t.Items.AddRange(td.Items);
            t.TotalDamage = td.TotalDamage;
            _gm.Towers.Add(t);
        }
        _gm.WaveTimer = 3;
        _gm.IsGameOver = false;
        _showRestoreDialog = false;
    }

    private void ScheduleSave()
    {
        _savePending = true;
        _saveTimer = 2f;
    }

    protected override void Update(GameTime gameTime)
    {
        var dt = (float)Math.Min(gameTime.ElapsedGameTime.TotalSeconds, 0.1);
        var ks = Keyboard.GetState();
        var ms = Mouse.GetState();
        _mouseX = ms.X; _mouseY = ms.Y;

        // ESC cancel (edge-triggered)
        bool escDown = ks.IsKeyDown(Keys.Escape);
        if (escDown && _prevEscape == ButtonState.Released)
        {
            if (_gm.SelectedTowerType != null || _gm.SelectedTower != null)
            {
                _gm.SelectedTowerType = null;
                _gm.SelectedTower = null;
                _gm.NotifyStateChanged();
            }
        }
        _prevEscape = escDown ? ButtonState.Pressed : ButtonState.Released;

        // P / Space toggle pause
        bool pDown = ks.IsKeyDown(Keys.P);
        if (pDown && _prevP == ButtonState.Released && !_gm.IsGameOver)
            _gm.IsPaused = !_gm.IsPaused;
        _prevP = pDown ? ButtonState.Pressed : ButtonState.Released;

        bool spaceDown = ks.IsKeyDown(Keys.Space);
        if (spaceDown && _prevSpace == ButtonState.Released && !_gm.IsGameOver)
            _gm.IsPaused = !_gm.IsPaused;
        _prevSpace = spaceDown ? ButtonState.Pressed : ButtonState.Released;

        // Restore dialog
        if (_showRestoreDialog)
        {
            bool yDown = ks.IsKeyDown(Keys.Y);
            if (yDown && _prevY == ButtonState.Released)
            {
                var data = SaveManager.Load();
                if (data != null) RestoreFromSave(data);
            }
            _prevY = yDown ? ButtonState.Pressed : ButtonState.Released;

            bool nDown = ks.IsKeyDown(Keys.N);
            if (nDown && _prevN == ButtonState.Released)
            {
                SaveManager.Clear();
                _showRestoreDialog = false;
            }
            _prevN = nDown ? ButtonState.Pressed : ButtonState.Released;
            base.Update(gameTime);
            return;
        }

        // Left click
        if (ms.LeftButton == ButtonState.Pressed && _prevLeft == ButtonState.Released)
        {
            if (_buildBar.HandleClick(_mouseX, _mouseY, _windowWidth, _windowHeight))
            {
                _gm.SelectedTowerType = _buildBar.ClickedType;
                _gm.SelectedTower = null;
                _gm.NotifyStateChanged();
            }
            else if (_mouseX >= _mapLeft && _mouseX <= _mapRight && _mouseY >= _mapTop && _mouseY <= _mapBottom)
            {
                _gm.HandleTap(_mouseX, _mouseY);
            }
        }
        _prevLeft = ms.LeftButton;

        // Right click cancel
        if (ms.RightButton == ButtonState.Pressed)
        {
            if (_gm.SelectedTowerType != null || _gm.SelectedTower != null)
            {
                _gm.SelectedTowerType = null;
                _gm.SelectedTower = null;
                _gm.NotifyStateChanged();
            }
        }

        _gm.HoverPos = new Vector2(_mouseX, _mouseY);
        _gm.Update(dt);

        if (_savePending)
        {
            _saveTimer -= dt;
            if (_saveTimer <= 0)
            {
                SaveManager.Save(_gm);
                _savePending = false;
            }
        }

        _fpsFrames++;
        _fpsAcc += dt;
        if (_fpsAcc >= 1)
        {
            _fps = _fpsFrames;
            _fpsFrames = 0;
            _fpsAcc = 0;
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(26, 42, 20));

        if (_font == null) return;

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        // Grass + Path (cached)
        _grassRenderer.Draw(_spriteBatch, (int)_mapLeft, (int)_mapTop);
        _pathRenderer.Draw(_spriteBatch);

        // Map border
        _spriteBatch.Draw(_texCache.BorderPixel, new Rectangle((int)_mapLeft - 2, (int)_mapTop - 2, (int)_mapW + 4, 2), Color.White);
        _spriteBatch.Draw(_texCache.BorderPixel, new Rectangle((int)_mapLeft - 2, (int)_mapBottom, (int)_mapW + 4, 2), Color.White);
        _spriteBatch.Draw(_texCache.BorderPixel, new Rectangle((int)_mapLeft - 2, (int)_mapTop - 2, 2, (int)(_mapBottom - _mapTop) + 4), Color.White);
        _spriteBatch.Draw(_texCache.BorderPixel, new Rectangle((int)_mapRight, (int)_mapTop - 2, 2, (int)(_mapBottom - _mapTop) + 4), Color.White);

        // Towers
        for (int i = 0; i < _gm.Towers.Count; i++)
        {
            var t = _gm.Towers[i];
            var isSelected = t == _gm.SelectedTower;

            if (isSelected)
            {
                var range = t.GetRange(_mapW);
                var ratio = range / 200f;
                var origin = new Vector2(200, 200);
                _spriteBatch.Draw(_texCache.RangeCircle, t.Position, null, Color.White, 0, origin, ratio, SpriteEffects.None, 0);
            }

            // Tower base platform
            var baseTex = _texCache.TowerCircles[t.Type + "_base"];
            var baseOrigin = new Vector2(baseTex.Width / 2, baseTex.Height / 2);
            _spriteBatch.Draw(baseTex, t.Position, null, Color.White, 0, baseOrigin, 1, SpriteEffects.None, 0);

            // Tower body (scaled with level)
            var size = MathF.Min(14 + t.Level * 1.5f, 26);
            var towerTex = _texCache.TowerCircles[t.Type];
            var scale = size / 26f;
            _spriteBatch.Draw(towerTex, t.Position, null, Color.White, 0, new Vector2(26, 26), scale, SpriteEffects.None, 0);

            // Level text on tower
            var lvText = t.IsUltimate() ? "★" : $"Lv{t.Level}";
            var lvSize = _font.MeasureString(lvText);
            _spriteBatch.DrawString(_font, lvText, t.Position - new Vector2(lvSize.X / 2, -size - 8), new Color(255, 215, 0));
        }

        // Monsters
        for (int i = 0; i < _gm.Monsters.Count; i++)
        {
            var m = _gm.Monsters[i];
            if (!m.Alive) continue;
            var r = (int)m.Radius;
            if (!_texCache.MonsterCircles.ContainsKey(r)) continue;
            var monsterTex = _texCache.MonsterCircles[r];
            var hpCol = ColorExtensions.HpColor(m.Hp / m.MaxHp);
            _spriteBatch.Draw(monsterTex, m.Position - new Vector2(r, r), hpCol);

            // HP bar
            if (m.MaxHp > 0)
            {
                var barW = 26; var barH = 4;
                var barX = m.Position.X - barW / 2; var barY = m.Position.Y - m.Radius - 9;
                var hpRatio = m.Hp / m.MaxHp;

                _spriteBatch.Draw(_texCache.HpBarBg, new Vector2(barX - 1, barY - 1), Color.White);
                _spriteBatch.Draw(_texCache.HpBarDark, new Vector2(barX, barY), Color.White);

                var fillW = Math.Max(1, (int)(barW * hpRatio));
                var hpFillTex = hpRatio > 0.66f ? _texCache.HpBarGreen :
                                hpRatio > 0.33f ? _texCache.HpBarYellow :
                                _texCache.HpBarRed;
                _spriteBatch.Draw(hpFillTex, new Vector2(barX, barY), new Rectangle(0, 0, fillW, barH), Color.White);
            }

            // Elite indicator
            if (m.IsElite && _font != null)
            {
                _spriteBatch.DrawString(_font, "★", new Vector2(m.Position.X - 5, m.Position.Y - m.Radius - 18), new Color(255, 180, 50));
            }
        }

        // Projectiles
        for (int i = 0; i < _gm.Projectiles.Count; i++)
        {
            var p = _gm.Projectiles[i];
            if (!p.Alive) continue;
            var tex = p.IsCrit ? _texCache.ProjectileCritTex : _texCache.ProjectileTex;
            var origin = new Vector2(tex.Width / 2, tex.Height / 2);
            var col = p.IsCrit ? Color.White : p.Tower.Def.Color;
            _spriteBatch.Draw(tex, p.Position, null, col, 0, origin, 1, SpriteEffects.None, 0);
        }

        // HUD
        _hud.Draw(_spriteBatch, _font, _gm, _windowWidth, _texCache);
        _buildBar.Draw(_spriteBatch, _font, _gm, _windowWidth, _windowHeight, _texCache);
        _infoPanel.Draw(_spriteBatch, _font, _gm, _windowWidth, _windowHeight, _texCache);

        // Overlays
        if (_gm.IsPaused)
            _overlays.DrawPause(_spriteBatch, _font, _windowWidth, _windowHeight, _texCache);
        if (_gm.IsGameOver)
            _overlays.DrawGameOver(_spriteBatch, _font, _gm, _windowWidth, _windowHeight, _texCache);

        // FPS
        var fpsColor = _fps >= 50 ? Color.Green : (_fps >= 30 ? Color.Yellow : Color.Red);
        _spriteBatch.DrawString(_font, $"FPS {_fps}", new Vector2(10, _windowHeight - 70), fpsColor);

        // Restore dialog
        if (_showRestoreDialog)
        {
            _spriteBatch.Draw(_texCache.OverlayRestore, Vector2.Zero, Color.White);
            _spriteBatch.DrawString(_font, _restoreInfo + "\n\n按 Y 恢复进度  按 N 新游戏",
                new Vector2(_windowWidth / 2 - 150, _windowHeight / 2 - 50), new Color(76, 175, 80));
        }

        _spriteBatch.End();
        base.Draw(gameTime);
    }
}