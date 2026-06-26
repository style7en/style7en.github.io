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
        _gm.BuildWaypoints(_windowWidth, _windowHeight, _mapLeft, _mapTop, _mapRight, _mapBottom);
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        try
        {
            _font = Content.Load<SpriteFont>("default");
        }
        catch
        {
            _font = FontFactory.CreateDefaultSpriteFont(GraphicsDevice);
        }

        _gm.OnStateChanged += () => ScheduleSave();
        _gm.OnRuleHint += (msg) => { };
        _gm.OnWaveNotice += (msg) => { };

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

        if (ks.IsKeyDown(Keys.Escape))
        {
            if (_gm.SelectedTowerType != null || _gm.SelectedTower != null)
            {
                _gm.SelectedTowerType = null;
                _gm.SelectedTower = null;
                _gm.NotifyStateChanged();
            }
        }

        bool pDown = ks.IsKeyDown(Keys.P);
        if (pDown && _prevP == ButtonState.Released && !_gm.IsGameOver)
        {
            _gm.IsPaused = !_gm.IsPaused;
        }
        _prevP = pDown ? ButtonState.Pressed : ButtonState.Released;

        bool spaceDown = ks.IsKeyDown(Keys.Space);
        if (spaceDown && _prevSpace == ButtonState.Released && !_gm.IsGameOver)
        {
            _gm.IsPaused = !_gm.IsPaused;
        }
        _prevSpace = spaceDown ? ButtonState.Pressed : ButtonState.Released;

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
            return;
        }

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

        _grassRenderer.Draw(_spriteBatch, (int)_mapLeft, (int)_mapTop);

        _pathRenderer.Draw(_spriteBatch);

        var borderTex = TextureGenerator.CreateRect(GraphicsDevice, 1, 1, new Color(10, 26, 5));
        _spriteBatch.Draw(borderTex, new Rectangle((int)_mapLeft - 2, (int)_mapTop - 2, (int)_mapW + 4, 2), Color.White);
        _spriteBatch.Draw(borderTex, new Rectangle((int)_mapLeft - 2, (int)_mapBottom, (int)_mapW + 4, 2), Color.White);
        _spriteBatch.Draw(borderTex, new Rectangle((int)_mapLeft - 2, (int)_mapTop - 2, 2, (int)(_mapBottom - _mapTop) + 4), Color.White);
        _spriteBatch.Draw(borderTex, new Rectangle((int)_mapRight, (int)_mapTop - 2, 2, (int)(_mapBottom - _mapTop) + 4), Color.White);

        for (int i = 0; i < _gm.Towers.Count; i++)
        {
            var t = _gm.Towers[i];
            var isSelected = t == _gm.SelectedTower;
            if (isSelected)
            {
                var range = t.GetRange(_mapW);
                var circleTex = TextureGenerator.CreateCircle(GraphicsDevice, (int)range, new Color(255, 255, 255, 77), false);
                _spriteBatch.Draw(circleTex, t.Position - new Vector2(range, range), Color.White);
            }
            var size = Math.Min(14 + t.Level * 1.5f, 26);
            var towerTex = TextureGenerator.CreateCircle(GraphicsDevice, (int)size, t.Def.Color);
            _spriteBatch.Draw(towerTex, t.Position - new Vector2(size, size), Color.White);
        }

        for (int i = 0; i < _gm.Monsters.Count; i++)
        {
            var m = _gm.Monsters[i];
            if (!m.Alive) continue;
            var col = ColorExtensions.HpColor(m.Hp / m.MaxHp);
            var colVal = new Color(col.R, col.G, col.B);
            var monsterTex = TextureGenerator.CreateCircle(GraphicsDevice, (int)m.Radius, colVal);
            _spriteBatch.Draw(monsterTex, m.Position - new Vector2(m.Radius, m.Radius), Color.White);
            if (m.MaxHp > 0)
            {
                var barW = 26; var barH = 4;
                var barX = m.Position.X - barW / 2; var barY = m.Position.Y - m.Radius - 9;
                var hpRatio = m.Hp / m.MaxHp;
                var hpColor = ColorExtensions.HpColor(hpRatio);
                var hpColVal = new Color(hpColor.R, hpColor.G, hpColor.B);
                var bgTex = TextureGenerator.CreateRect(GraphicsDevice, barW + 2, barH + 2, new Color(0, 0, 0, 140));
                var barBgTex = TextureGenerator.CreateRect(GraphicsDevice, barW, barH, new Color(51, 51, 51));
                var hpTex = TextureGenerator.CreateRect(GraphicsDevice, Math.Max(1, (int)(barW * hpRatio)), barH, hpColVal);
                _spriteBatch.Draw(bgTex, new Vector2(barX - 1, barY - 1), Color.White);
                _spriteBatch.Draw(barBgTex, new Vector2(barX, barY), Color.White);
                _spriteBatch.Draw(hpTex, new Vector2(barX, barY), Color.White);
            }
        }

        _hud.Draw(_spriteBatch, _font, _gm, _windowWidth);
        _buildBar.Draw(_spriteBatch, _font, _gm, _windowWidth, _windowHeight);
        _infoPanel.Draw(_spriteBatch, _font, _gm, _windowWidth, _windowHeight);

        if (_gm.IsPaused)
            _overlays.DrawPause(_spriteBatch, _font, _windowWidth, _windowHeight);
        if (_gm.IsGameOver)
            _overlays.DrawGameOver(_spriteBatch, _font, _gm, _windowWidth, _windowHeight);

        var fpsColor = _fps >= 50 ? Color.Green : (_fps >= 30 ? Color.Yellow : Color.Red);
        _spriteBatch.DrawString(_font, $"FPS {_fps}", new Vector2(10, _windowHeight - 70), fpsColor);

        if (_showRestoreDialog)
        {
            var overlayTex = TextureGenerator.CreateRect(GraphicsDevice, _windowWidth, _windowHeight, new Color(0, 0, 0, 204));
            _spriteBatch.Draw(overlayTex, Vector2.Zero, Color.White);
            _spriteBatch.DrawString(_font, _restoreInfo + "\n\n按 Y 恢复进度  按 N 新游戏",
                new Vector2(_windowWidth / 2 - 150, _windowHeight / 2 - 50), new Color(76, 175, 80));
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
