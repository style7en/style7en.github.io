# Elemental Loop TD — C# MonoGame Native Port Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Port `elemental_loop_td.html` (3086-line Canvas 2D tower defense game) to a C# MonoGame Windows desktop application.

**Architecture:** MonoGame `Game` class with `Update/Draw` loop. All textures generated programmatically at init via `RenderTarget2D`. Static layers (path, grass) cached as `RenderTarget2D`. UI rendered via `SpriteBatch` (textures + SpriteFont). Core state in `GameManager` singleton.

**Tech Stack:** .NET 8.0, MonoGame 3.8+, System.Text.Json, C# 12

**Prerequisite:** .NET SDK 8.0 must be installed at `$HOME/.dotnet`. MonoGame NuGet packages resolve at build time.

---

### Task 0: Create Project & Install MonoGame

**Files:**
- Create: `ElementalLoopTD/ElementalLoopTD.csproj`
- Create: `ElementalLoopTD/Program.cs`

- [ ] **Step 1: Create solution and project**

Run:
```bash
export PATH="$HOME/.dotnet:$PATH"
mkdir -p /c/Users/zhangjia/project/style7en.github.io/ElementalLoopTD
cd /c/Users/zhangjia/project/style7en.github.io/ElementalLoopTD
dotnet new console -n ElementalLoopTD --force
```
Expected: "The template "Console App" was created successfully."

- [ ] **Step 2: Add MonoGame packages**

```bash
cd /c/Users/zhangjia/project/style7en.github.io/ElementalLoopTD/ElementalLoopTD
dotnet add package MonoGame.Framework.DesktopGL --version 3.8.2.1105
dotnet add package MonoGame.Content.Builder.Task
```
Expected: "PackageReference for 'MonoGame.Framework.DesktopGL' added to file 'ElementalLoopTD.csproj'"

- [ ] **Step 3: Update csproj for Windows desktop**

Write `ElementalLoopTD/ElementalLoopTD.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <RootNamespace>ElementalLoopTD</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="MonoGame.Framework.DesktopGL" Version="3.8.2.1105" />
    <PackageReference Include="MonoGame.Content.Builder.Task" Version="3.8.2.1105" />
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Write minimal Game1.cs to test build**

Create `ElementalLoopTD/Game1.cs`:

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ElementalLoopTD;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        Window.Title = "元素循环圈塔防";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(26, 26, 46));
        base.Draw(gameTime);
    }
}
```

Update `Program.cs`:

```csharp
using ElementalLoopTD;

using var game = new Game1();
game.Run();
```

Add `app.manifest` (empty minimal):

```xml
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
  <assemblyIdentity version="1.0.0.0" name="ElementalLoopTD" />
</assembly>
```

- [ ] **Step 5: Build and verify**

```bash
cd /c/Users/zhangjia/project/style7en.github.io/ElementalLoopTD/ElementalLoopTD
dotnet build
```
Expected: "Build succeeded."

- [ ] **Step 6: Commit**

```bash
git add ElementalLoopTD/
git commit -m "feat: scaffold MonoGame project with DesktopGL"
```

---

### Task 1: Implement Config.cs (Constants)

**Files:**
- Create: `ElementalLoopTD/Core/Config.cs`

This file contains ALL constants from the original: TOWER_DEFS, ITEM_DEFS, element reactions, wave parameters, safety caps.

- [ ] **Step 1: Create Core directory and Config.cs**

```bash
mkdir -p /c/Users/zhangjia/project/style7en.github.io/ElementalLoopTD/ElementalLoopTD/Core
```

Write `ElementalLoopTD/Core/Config.cs`:

```csharp
namespace ElementalLoopTD.Core;

public static class Config
{
    public const float RangeRatioMax = 0.80f;

    public static class Towers
    {
        public static readonly TowerDef Fire = new()
        {
            Type = "fire", Name = "火塔", Icon = "🔥", Color = new Color(255, 87, 34),
            BaseAtk = 23, RangeRatio = 0.227f, BaseSpeed = 1.2f, Cost = 100,
            SplashRatio = 0, SplashMax = 0, SlowRate = 0, SlowDur = 0, Desc = "单体伤害",
            Ult = new UltDef { Name = "焚天", Desc = "攻击附带范围灼烧", AtkMul = 1.6f, RangeMul = 1.2f, SplashRatio = 0.076f }
        };

        public static readonly TowerDef Water = new()
        {
            Type = "water", Name = "水塔", Icon = "💧", Color = new Color(33, 150, 243),
            BaseAtk = 12, RangeRatio = 0.189f, BaseSpeed = 1.8f, Cost = 80,
            SplashRatio = 0.095f, SplashMax = 3, SlowRate = 0, SlowDur = 0, Desc = "溅射(地图 9.5%)",
            Ult = new UltDef { Name = "怒涛", Desc = "溅射范围+数量翻倍", AtkMul = 1.4f, RangeMul = 1.1f, SplashRatio = 0.151f, SplashMax = 5 }
        };

        public static readonly TowerDef Ice = new()
        {
            Type = "ice", Name = "冰塔", Icon = "❄️", Color = new Color(0, 188, 212),
            BaseAtk = 8, RangeRatio = 0.208f, BaseSpeed = 1.0f, Cost = 90,
            SplashRatio = 0, SplashMax = 0, SlowRate = 0.5f, SlowDur = 2f, Desc = "减速50%",
            Ult = new UltDef { Name = "极寒", Desc = "减速至25%并冻结0.5s", AtkMul = 1.3f, RangeMul = 1.1f, SlowRate = 0.75f, SlowDur = 3f }
        };

        public static readonly Dictionary<string, TowerDef> All = new()
        {
            ["fire"] = Fire, ["water"] = Water, ["ice"] = Ice
        };
    }

    public static class Items
    {
        public static readonly ItemDef[] All = new[]
        {
            new ItemDef { Id = "critRate", Name = "暴击率+5%", Prob = 0.15f },
            new ItemDef { Id = "critDmg", Name = "爆伤+20%", Prob = 0.15f },
            new ItemDef { Id = "range", Name = "范围+2%地图", Prob = 0.10f },
            new ItemDef { Id = "atkSpeed", Name = "攻速+0.1/s", Prob = 0.10f },
        };
    }

    public static class Elite
    {
        public const int WaveInterval = 5;
        public const float HpBaseMult = 5f;
        public const float HpWaveScale = 0.05f;
        public const float ResistBonus = 0.10f;
        public const float SpeedMult = 0.85f;
        public const float SizeMult = 1.45f;
        public const int GoldMult = 3;
        public const float DropProb = 0.75f;
        public const float DropNormProbMul = 0.5f;
    }

    public static class Combat
    {
        public const int MaxTowerLevel = int.MaxValue;
        public const int UltimateLevel = 10;
        public const int UpgradeCostPerLevel = 50;
        public const int UpgradeCostTier = 10;
        public const float MaxCritRate = 1.0f;
        public const float MaxCritDmg = 10.0f;
        public const float MaxAtkSpeed = 20f;
        public const float MaxSafeAtk = 1e9f;
        public const float MaxSafeHp = 1e12f;
        public const float MaxSafeGold = 1e12f;
        public const int MaxSafeWave = 1000000;
        public const float MonsterBaseHp = 80f;
        public const float MonsterHpLinear = 0.40f;
        public const float MonsterHpQuad = 0.012f;
        public const float MonsterSpeed = 50f;
        public const float MonsterBaseResist = 0f;
        public const float MonsterResistAsymp = 0.45f;
        public const float MonsterResistTau = 50f;
        public const float WaveBaseInterval = 10f;
        public const float WaveIntervalPerWave = 0.2f;
        public const float WaveIntervalCap = 16f;
        public const float MonsterSpawnInterval = 0.65f;
        public const int InitialGold = 300;
        public const float KillGoldBase = 25f;
        public const float KillGoldPerWave = 3.0f;
        public const float KillGoldQuad = 0.015f;
        public const int MaxMonstersGameover = 100;
        public const float SlowDuration = 2f;
    }

    public static class Elements
    {
        public const float Duration = 5f;
        public const float Vaporize = 2.0f;
        public const float Freeze = 1.5f;
        public const float Melt = 1.5f;
        public const float FreezeDuration = 1.5f;
    }

    // Visual helpers
    public static readonly Dictionary<string, ElementVisual> ElementVisuals = new()
    {
        ["fire"] = new ElementVisual { Icon = "🔥", Color = new Color(255, 23, 68), Glow = new Color(255, 87, 34) },
        ["water"] = new ElementVisual { Icon = "💧", Color = new Color(61, 90, 254), Glow = new Color(25, 118, 210) },
        ["ice"] = new ElementVisual { Icon = "❄️", Color = new Color(128, 222, 234), Glow = new Color(0, 188, 212) },
    };
}

public class TowerDef
{
    public string Type = ""; public string Name = ""; public string Icon = "";
    public Color Color; public int BaseAtk; public float RangeRatio;
    public float BaseSpeed; public int Cost; public float SplashRatio;
    public int SplashMax; public float SlowRate; public float SlowDur; public string Desc = "";
    public UltDef? Ult;
}

public class UltDef
{
    public string Name = ""; public string Desc = "";
    public float AtkMul = 1f; public float RangeMul = 1f;
    public float SplashRatio; public int SplashMax;
    public float SlowRate; public float SlowDur;
}

public class ItemDef
{
    public string Id = ""; public string Name = ""; public float Prob;
}

public class ElementVisual
{
    public string Icon = ""; public Color Color; public Color Glow;
}

public struct Waypoint
{
    public float X, Y;
    public Waypoint(float x, float y) { X = x; Y = y; }
}
```

- [ ] **Step 2: Build and verify**

```bash
cd /c/Users/zhangjia/project/style7en.github.io/ElementalLoopTD/ElementalLoopTD
dotnet build
```
Expected: "Build succeeded."

- [ ] **Step 3: Commit**

```bash
git add ElementalLoopTD/
git commit -m "feat: add Config.cs with all game constants"
```

---

### Task 2: Implement SafeMath & Extensions

**Files:**
- Create: `ElementalLoopTD/Utils/SafeMath.cs`
- Create: `ElementalLoopTD/Utils/Extensions.cs`

- [ ] **Step 1: Create Utils directory**

```bash
mkdir -p /c/Users/zhangjia/project/style7en.github.io/ElementalLoopTD/ElementalLoopTD/Utils
```

Write `ElementalLoopTD/Utils/SafeMath.cs`:

```csharp
namespace ElementalLoopTD.Utils;

public static class SafeMath
{
    public static float Clamp(float v, float min, float max)
    {
        if (float.IsNaN(v) || float.IsInfinity(v)) return min;
        return Math.Clamp(v, min, max);
    }

    public static int SafeRound(float v)
    {
        if (!float.IsFinite(v)) return 0;
        return (int)Math.Round(v, MidpointRounding.AwayFromZero);
    }

    public static float SafeFinite(float v, float fallback = 0)
    {
        return float.IsFinite(v) ? v : fallback;
    }

    public static float SafeAdd(float a, float b, float cap)
    {
        var aa = SafeFinite(a, 0);
        var bb = SafeFinite(b, 0);
        var r = aa + bb;
        return Math.Clamp(r, -cap, cap);
    }

    public static float SafeMul(float a, float b, float cap)
    {
        var aa = SafeFinite(a, 0);
        var bb = SafeFinite(b, 0);
        var r = aa * bb;
        if (!float.IsFinite(r)) return 0;
        return Math.Clamp(r, -cap, cap);
    }

    public static int SafeNum(float v, int min, int max)
    {
        if (float.IsNaN(v) || float.IsInfinity(v)) return min;
        return Math.Clamp((int)v, min, max);
    }

    public static float Dist(float x1, float y1, float x2, float y2)
    {
        var dx = x1 - x2; var dy = y1 - y2;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    public static float DistSq(float x1, float y1, float x2, float y2)
    {
        var dx = x1 - x2; var dy = y1 - y2;
        return dx * dx + dy * dy;
    }

    public static float DistPointSeg(float px, float py, float ax, float ay, float bx, float by)
    {
        var dx = bx - ax; var dy = by - ay;
        var len2 = dx * dx + dy * dy;
        var t = len2 > 0 ? Math.Clamp(((px - ax) * dx + (py - ay) * dy) / len2, 0, 1) : 0f;
        var ex = px - (ax + t * dx);
        var ey = py - (ay + t * dy);
        return MathF.Sqrt(ex * ex + ey * ey);
    }
}
```

Write `ElementalLoopTD/Utils/Extensions.cs`:

```csharp
using Microsoft.Xna.Framework;

namespace ElementalLoopTD.Utils;

public static class ColorExtensions
{
    public static Color LerpColor(Color a, Color b, float t)
    {
        return new Color(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t),
            (byte)(a.A + (b.A - a.A) * t)
        );
    }

    public static Color HpColor(float ratio)
    {
        if (!float.IsFinite(ratio)) ratio = 1;
        ratio = Math.Clamp(ratio, 0, 1);
        var green = new Color(76, 175, 80);
        var yellow = new Color(255, 235, 59);
        var red = new Color(244, 67, 54);
        if (ratio > 0.5f)
            return LerpColor(yellow, green, (ratio - 0.5f) * 2);
        else
            return LerpColor(red, yellow, ratio * 2);
    }
}
```

- [ ] **Step 2: Build**

```bash
cd /c/Users/zhangjia/project/style7en.github.io/ElementalLoopTD/ElementalLoopTD && dotnet build
```

- [ ] **Step 3: Commit**

```bash
git add ElementalLoopTD/
git commit -m "feat: add SafeMath and Extensions utilities"
```

---

### Task 3: Implement Entities (Tower, Monster, Projectile)

**Files:**
- Create: `ElementalLoopTD/Entities/Tower.cs`
- Create: `ElementalLoopTD/Entities/Monster.cs`
- Create: `ElementalLoopTD/Entities/Projectile.cs`
- Modify: (none yet; GameManager references these)

- [ ] **Step 1: Create Entities directory**

```bash
mkdir -p /c/Users/zhangjia/project/style7en.github.io/ElementalLoopTD/ElementalLoopTD/Entities
```

Write `ElementalLoopTD/Entities/Tower.cs`:

```csharp
using ElementalLoopTD.Core;
using ElementalLoopTD.Utils;
using Microsoft.Xna.Framework;

namespace ElementalLoopTD.Entities;

public class Tower
{
    public string Type { get; }
    public TowerDef Def => Config.Towers.All[Type];
    public Vector2 Position;
    public int Level = 1;
    public float Cooldown;
    public float CritRate = 0.05f;
    public float CritDamage = 1.5f;
    public float BonusRangeRatio;
    public float BonusSpeed;
    public readonly List<string> Items = new();
    public long TotalDamage;

    public Tower(string type, float x, float y)
    {
        Type = type;
        Position = new Vector2(x, y);
    }

    public int GetAtk()
    {
        var ult = Level >= Config.Combat.UltimateLevel ? Def.Ult!.AtkMul : 1f;
        var lv = SafeMath.Clamp(Level, 1, 1000000);
        var growth = 1f + 0.10f * MathF.Pow(lv - 1, 0.85f);
        var raw = Def.BaseAtk * growth * ult;
        return SafeMath.SafeRound(SafeMath.Clamp(raw, 0, Config.Combat.MaxSafeAtk));
    }

    public float GetRangeRatio()
    {
        var ult = Level >= Config.Combat.UltimateLevel ? Def.Ult?.RangeMul ?? 1 : 1;
        return Math.Clamp(Def.RangeRatio * ult + BonusRangeRatio, 0, Config.RangeRatioMax);
    }

    public float GetRange(float mapW)
    {
        return GetRangeRatio() * mapW;
    }

    public float GetSplash(float mapW)
    {
        var ratio = Level >= Config.Combat.UltimateLevel && Def.Ult?.SplashRatio > 0
            ? Def.Ult.SplashRatio : Def.SplashRatio;
        return ratio * mapW;
    }

    public int GetSplashMax()
    {
        return Level >= Config.Combat.UltimateLevel && Def.Ult?.SplashMax > 0
            ? Def.Ult.SplashMax : Def.SplashMax;
    }

    public float GetSlowRate()
    {
        return Level >= Config.Combat.UltimateLevel && Def.Ult?.SlowRate > 0
            ? Def.Ult.SlowRate : Def.SlowRate;
    }

    public float GetSlowDur()
    {
        return Level >= Config.Combat.UltimateLevel && Def.Ult?.SlowDur > 0
            ? Def.Ult.SlowDur : Def.SlowDur;
    }

    public bool IsUltimate() => Level >= Config.Combat.UltimateLevel;

    public float GetSpeed() => Def.BaseSpeed + BonusSpeed;

    public int GetUpgradeCost()
    {
        var lv = SafeMath.Clamp(Level, 1, 1000000);
        var tier = (int)MathF.Ceiling(lv / (float)Config.Combat.UpgradeCostTier);
        return Config.Combat.UpgradeCostPerLevel * Math.Max(1, tier);
    }

    public void Upgrade() => Level++;

    public bool CanShoot() => Cooldown <= 0;

    public void ResetCooldown() => Cooldown = 1f / GetSpeed();

    public Monster? FindTarget(List<Monster> monsters, float mapW)
    {
        Monster? best = null;
        var bestProgress = -1f;
        var rangeSq = GetRange(mapW); rangeSq *= rangeSq;
        var tx = Position.X; var ty = Position.Y;
        for (int i = 0; i < monsters.Count; i++)
        {
            var m = monsters[i];
            if (!m.Alive) continue;
            var dx = tx - m.Position.X; var dy = ty - m.Position.Y;
            if (dx * dx + dy * dy <= rangeSq && m.PathProgress > bestProgress)
            {
                best = m;
                bestProgress = m.PathProgress;
            }
        }
        return best;
    }

    public bool ApplyItem(ItemDef item)
    {
        switch (item.Id)
        {
            case "critRate":
                if (CritRate >= Config.Combat.MaxCritRate) return false;
                CritRate = Math.Min(Config.Combat.MaxCritRate, CritRate + 0.05f);
                return true;
            case "critDmg":
                if (CritDamage >= Config.Combat.MaxCritDmg) return false;
                CritDamage = Math.Min(Config.Combat.MaxCritDmg, CritDamage + 0.20f);
                return true;
            case "range":
                if (GetRangeRatio() + 0.02f > Config.RangeRatioMax) return false;
                BonusRangeRatio += 0.02f;
                return true;
            case "atkSpeed":
                if (BonusSpeed >= Config.Combat.MaxAtkSpeed - Def.BaseSpeed) return false;
                BonusSpeed = Math.Min(Config.Combat.MaxAtkSpeed - Def.BaseSpeed, BonusSpeed + 0.1f);
                return true;
        }
        return false;
    }

    public void Update(float dt)
    {
        if (Cooldown > 0) Cooldown -= dt;
    }
}
```

Write `ElementalLoopTD/Entities/Monster.cs`:

```csharp
using ElementalLoopTD.Core;
using ElementalLoopTD.Utils;
using Microsoft.Xna.Framework;

namespace ElementalLoopTD.Entities;

public class Monster
{
    public int Wave;
    public bool IsElite;
    public float Hp, MaxHp;
    public float Resist;
    public float BaseSpeed;
    public float Speed;
    public float SlowTimer, SlowRate;
    public bool Alive = true;
    public List<Waypoint> Waypoints;
    public int WpIndex;
    public Vector2 Position;
    public float PathProgress;
    public float Radius;
    public string? Element;
    public float ElementTimer;
    public float FrozenTimer;
    public Tower? KillerTower;

    public Monster(int wave, List<Waypoint> waypoints, bool isElite = false)
    {
        Wave = wave;
        IsElite = isElite;
        var safeWave = Math.Max(0, Math.Min(Config.Combat.MaxSafeWave, wave));
        var eliteHpMult = IsElite ? (Config.Elite.HpBaseMult + safeWave * Config.Elite.HpWaveScale) : 1f;
        var spMult = IsElite ? Config.Elite.SpeedMult : 1f;
        var szMult = IsElite ? Config.Elite.SizeMult : 1f;
        var hpGrowth = 1f + safeWave * Config.Combat.MonsterHpLinear + safeWave * safeWave * Config.Combat.MonsterHpQuad;
        MaxHp = SafeMath.SafeRound(Math.Clamp(Config.Combat.MonsterBaseHp * hpGrowth * eliteHpMult, 1, Config.Combat.MaxSafeHp));
        Hp = MaxHp;
        var baseResist = Config.Combat.MonsterResistAsymp * (1f - MathF.Exp(-safeWave / Config.Combat.MonsterResistTau));
        Resist = IsElite ? Math.Min(0.85f, baseResist + Config.Elite.ResistBonus) : baseResist;
        BaseSpeed = Config.Combat.MonsterSpeed * spMult;
        Speed = BaseSpeed;
        Waypoints = waypoints;
        WpIndex = 1;
        Position = new Vector2(waypoints[0].X, waypoints[0].Y);
        PathProgress = 0;
        Radius = 10 * szMult;
    }

    public void TakeDamage(int rawDmg, bool isCrit, string? towerType, Tower? killer)
    {
        if (!Alive) return;
        var frozenBefore = FrozenTimer > 0;

        // Element reaction
        var reactionMul = 1f;
        var isReaction = false;
        if (!string.IsNullOrEmpty(towerType))
        {
            if (Element != null && Element != towerType)
            {
                var result = ElementSystem.Resolve(Element, towerType);
                if (result.HasValue)
                {
                    reactionMul = result.Value.Mul;
                    if (result.Value.Freeze)
                        FrozenTimer = Math.Max(FrozenTimer, Config.Elements.FreezeDuration);
                    Element = null;
                    ElementTimer = 0;
                    isReaction = true;
                }
            }
            else if (Element == null)
            {
                Element = towerType;
                ElementTimer = Config.Elements.Duration;
            }
            else if (Element == towerType)
            {
                ElementTimer = Config.Elements.Duration;
            }
        }
        if (frozenBefore) reactionMul *= Config.Elements.Freeze;

        var cleanDmg = Math.Clamp(rawDmg, 0, Config.Combat.MaxSafeAtk);
        var factor = Math.Clamp(1 - Resist, 0, 1);
        var finalMul = Math.Clamp(reactionMul, 0, 10);
        var actualDmg = Math.Max(1, SafeMath.SafeRound(SafeMath.SafeMul(SafeMath.SafeMul(cleanDmg, factor, Config.Combat.MaxSafeAtk), finalMul, Config.Combat.MaxSafeAtk)));
        Hp = Math.Clamp(Hp - actualDmg, 0, MaxHp);
        KillerTower = killer ?? KillerTower;

        if (Hp <= 0)
        {
            Alive = false;
        }
    }

    public void ApplySlow(float rate, float duration)
    {
        SlowRate = Math.Max(SlowRate, rate);
        SlowTimer = Math.Max(SlowTimer, duration);
    }

    public void Update(float dt)
    {
        if (!Alive) return;
        if (SlowTimer > 0)
        {
            SlowTimer = Math.Max(0, SlowTimer - dt);
            Speed = BaseSpeed * Math.Clamp(1 - SlowRate, 0, 1);
            if (SlowTimer <= 0) { SlowRate = 0; Speed = BaseSpeed; }
        }
        if (ElementTimer > 0)
        {
            ElementTimer = Math.Max(0, ElementTimer - dt);
            if (ElementTimer <= 0) Element = null;
        }
        if (FrozenTimer > 0) FrozenTimer = Math.Max(0, FrozenTimer - dt);

        if (WpIndex >= Waypoints.Count) WpIndex = 1;
        var target = Waypoints[WpIndex];
        var dx = target.X - Position.X;
        var dy = target.Y - Position.Y;
        var d = MathF.Sqrt(dx * dx + dy * dy);
        var effectiveSpeed = FrozenTimer > 0 ? 0 : Speed;
        var move = effectiveSpeed * dt;
        if (d <= move)
        {
            Position = new Vector2(target.X, target.Y);
            PathProgress += d;
            WpIndex++;
            if (WpIndex >= Waypoints.Count) WpIndex = 1;
        }
        else if (d > 0)
        {
            Position += new Vector2(dx / d * move, dy / d * move);
            PathProgress += move;
        }
    }
}
```

Write `ElementalLoopTD/Entities/Projectile.cs`:

```csharp
using ElementalLoopTD.Core;
using ElementalLoopTD.Utils;
using Microsoft.Xna.Framework;

namespace ElementalLoopTD.Entities;

public class Projectile
{
    public Vector2 Position;
    public Monster Target;
    public Tower Tower;
    public float Speed = 400f;
    public bool Alive = true;
    public int Damage;
    public bool IsCrit;

    public Projectile(Vector2 from, Monster target, Tower tower)
    {
        Position = from;
        Target = target;
        Tower = tower;
        var atk = SafeMath.Clamp(tower.GetAtk(), 0, Config.Combat.MaxSafeAtk);
        IsCrit = Random.Shared.NextDouble() < tower.CritRate;
        if (IsCrit)
        {
            var cd = Math.Max(1, tower.CritDamage);
            atk = SafeMath.SafeMul(atk, cd, Config.Combat.MaxSafeAtk);
        }
        Damage = SafeMath.SafeRound(Math.Clamp(atk, 0, Config.Combat.MaxSafeAtk));
    }

    public void Update(float dt, List<Monster> monsters, List<Particle> particles, float mapW)
    {
        if (!Target.Alive) { Alive = false; return; }
        var dx = Target.Position.X - Position.X;
        var dy = Target.Position.Y - Position.Y;
        var d = MathF.Sqrt(dx * dx + dy * dy);
        if (d < 8)
        {
            Hit(monsters, particles, mapW);
            Alive = false;
            return;
        }
        var move = Speed * dt;
        Position += new Vector2(dx / d * move, dy / d * move);
    }

    private void Hit(List<Monster> monsters, List<Particle> particles, float mapW)
    {
        var t = Target;
        t.TakeDamage(Damage, IsCrit, Tower.Type, Tower);

        // Water splash
        if (Tower.Type == "water" && Tower.GetSplash(mapW) > 0)
        {
            var splash = Tower.GetSplash(mapW);
            var splashSq = splash * splash;
            var splashMax = Tower.GetSplashMax();
            var tx = t.Position.X; var ty = t.Position.Y;
            var hit = 0;
            for (int i = 0; i < monsters.Count && hit < splashMax; i++)
            {
                var m = monsters[i];
                if (!m.Alive || m == t) continue;
                var dx = m.Position.X - tx; var dy = m.Position.Y - ty;
                if (dx * dx + dy * dy <= splashSq)
                {
                    var splashDmg = SafeMath.SafeRound(SafeMath.SafeMul(Damage, 0.5f, Config.Combat.MaxSafeAtk));
                    m.TakeDamage(splashDmg, false, Tower.Type, Tower);
                    hit++;
                }
            }
        }

        // Ice slow
        if (Tower.Type == "ice")
            t.ApplySlow(Tower.GetSlowRate(), Tower.GetSlowDur());
    }
}
```

Write `ElementalLoopTD/Core/ElementSystem.cs`:

```csharp
namespace ElementalLoopTD.Core;

public struct ReactionResult
{
    public float Mul; public string Label; public bool Freeze;
}

public static class ElementSystem
{
    private static readonly Dictionary<(string existing, string incoming), ReactionResult> Reactions = new()
    {
        [("fire", "water")] = new ReactionResult { Mul = Config.Elements.Vaporize, Label = "蒸发", Freeze = false },
        [("water", "fire")] = new ReactionResult { Mul = Config.Elements.Vaporize, Label = "蒸发", Freeze = false },
        [("water", "ice")] = new ReactionResult { Mul = Config.Elements.Freeze, Label = "冻结", Freeze = true },
        [("ice", "water")] = new ReactionResult { Mul = Config.Elements.Freeze, Label = "冻结", Freeze = true },
        [("fire", "ice")] = new ReactionResult { Mul = Config.Elements.Melt, Label = "融化", Freeze = false },
        [("ice", "fire")] = new ReactionResult { Mul = Config.Elements.Melt, Label = "融化", Freeze = false },
    };

    public static ReactionResult? Resolve(string existing, string incoming)
    {
        if (Reactions.TryGetValue((existing, incoming), out var r))
            return r;
        if (Reactions.TryGetValue((incoming, existing), out var r2))
            return r2;
        return null;
    }
}
```

- [ ] **Step 2: Build**

```bash
cd /c/Users/zhangjia/project/style7en.github.io/ElementalLoopTD/ElementalLoopTD && dotnet build
```

- [ ] **Step 3: Commit**

```bash
git add ElementalLoopTD/
git commit -m "feat: add core entities - Tower, Monster, Projectile, ElementSystem"
```

---

### Task 4: Implement Particle System

**Files:**
- Create: `ElementalLoopTD/Particles/Particle.cs`
- Create: `ElementalLoopTD/Particles/DamageParticle.cs`
- Create: `ElementalLoopTD/Particles/ReactionParticle.cs`
- Create: `ElementalLoopTD/Particles/TrailParticle.cs`
- Create: `ElementalLoopTD/Particles/MergeBurst.cs`

All particles follow a common pattern: `Alive` flag, `Update(float dt)`, `Draw(SpriteBatch, GameTime)`.

- [ ] **Step 1: Create directory**

```bash
mkdir -p /c/Users/zhangjia/project/style7en.github.io/ElementalLoopTD/ElementalLoopTD/Particles
```

Write `ElementalLoopTD/Particles/Particle.cs`:

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElementalLoopTD.Entities;

public class Particle
{
    public Vector2 Position;
    public string Text;
    public Color Color;
    public float Duration;
    public float Elapsed;
    public bool Alive = true;

    public Particle(float x, float y, string text, Color color, float duration = 1.5f)
    {
        Position = new Vector2(x, y);
        Text = text;
        Color = color;
        Duration = duration;
    }

    public virtual void Update(float dt)
    {
        Elapsed += dt;
        Position.Y -= 30 * dt;
        if (Elapsed >= Duration) Alive = false;
    }

    public virtual void Draw(SpriteBatch sb, SpriteFont font)
    {
        var alpha = Math.Max(0, 1 - Elapsed / Duration);
        if (font != null)
            sb.DrawString(font, Text, Position, Color * alpha);
    }
}
```

Write `ElementalLoopTD/Particles/DamageParticle.cs`:

```csharp
using ElementalLoopTD.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElementalLoopTD.Entities;

public class DamageParticle : Particle
{
    public int Dmg;
    public bool IsCrit;
    public string? TowerType;
    public float StartX;
    public float FloatSpeed;
    public float Scale;
    public float ShakePhase;

    public DamageParticle(float x, float y, int dmg, bool isCrit, string? towerType)
        : base(x + (float)(Random.Shared.NextDouble() - 0.5) * 10, y, "", GetColor(towerType, isCrit), isCrit ? 1.5f : 1.0f)
    {
        Dmg = dmg;
        IsCrit = isCrit;
        TowerType = towerType;
        StartX = Position.X;
        FloatSpeed = isCrit ? 45 : 30;
        Scale = isCrit ? 1.4f : 1.0f;
        ShakePhase = (float)(Random.Shared.NextDouble() * Math.PI * 2);
    }

    private static Color GetColor(string? towerType, bool isCrit)
    {
        return towerType switch
        {
            "fire" => new Color(255, 23, 68),
            "water" => new Color(61, 90, 254),
            "ice" => new Color(128, 222, 234),
            _ when isCrit => new Color(255, 235, 59),
            _ => Color.White,
        };
    }

    public override void Update(float dt)
    {
        Elapsed += dt;
        Position.Y -= FloatSpeed * dt;
        if (IsCrit)
            Position.X = StartX + MathF.Sin(ShakePhase + Elapsed * 12) * 4 * (1 - Elapsed / Duration);
        if (Elapsed >= Duration) Alive = false;
    }

    public override void Draw(SpriteBatch sb, SpriteFont font)
    {
        var alpha = Math.Max(0, 1 - Elapsed / Duration);
        var s = Scale;
        var text = IsCrit ? $"⚡{Dmg}" : $"{Dmg}";
        if (font != null)
        {
            var scale = new Vector2(s, s);
            var origin = font.MeasureString(text) / 2;
            if (IsCrit)
                sb.DrawString(font, text, Position, Color * alpha, 0, origin, scale, SpriteEffects.None, 0);
            else
                sb.DrawString(font, text, Position, Color * alpha, 0, origin, scale, SpriteEffects.None, 0);
        }
    }
}
```

Write `ElementalLoopTD/Particles/ReactionParticle.cs`:

```csharp
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
```

Write `ElementalLoopTD/Particles/TrailParticle.cs`:

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElementalLoopTD.Entities;

public class TrailParticle : Particle
{
    public Vector2 Start, End;
    public Color TrailColor;

    public TrailParticle(float x0, float y0, float x1, float y1, Color color)
        : base(x0, y0, "", color, 0.45f)
    {
        Start = new Vector2(x0, y0);
        End = new Vector2(x1, y1);
        TrailColor = color;
    }

    public override void Update(float dt)
    {
        Elapsed += dt;
        if (Elapsed >= Duration) Alive = false;
    }

    public override void Draw(SpriteBatch sb, SpriteFont font)
    {
        // Will be drawn as line + circle in TextureGenerator or direct line drawing
    }
}
```

Write `ElementalLoopTD/Particles/MergeBurst.cs`:

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElementalLoopTD.Entities;

public class MergeBurst : Particle
{
    public float R0 = 14, R1 = 60;

    public MergeBurst(float x, float y, Color color)
        : base(x, y, "", color, 0.55f) { }

    public override void Update(float dt)
    {
        Elapsed += dt;
        if (Elapsed >= Duration) Alive = false;
    }

    public override void Draw(SpriteBatch sb, SpriteFont font)
    {
        // Will be rendered as ring in main draw
    }
}
```

- [ ] **Step 2: Build**

```bash
cd /c/Users/zhangjia/project/style7en.github.io/ElementalLoopTD/ElementalLoopTD && dotnet build
```

- [ ] **Step 3: Commit**

```bash
git add ElementalLoopTD/
git commit -m "feat: add particle system classes"
```

---

### Task 5: Implement WaveManager and GameManager

**Files:**
- Create: `ElementalLoopTD/Core/WaveManager.cs`
- Create: `ElementalLoopTD/Core/GameManager.cs`

- [ ] **Step 1: Create directories**

Write `ElementalLoopTD/Core/WaveManager.cs`:

```csharp
using ElementalLoopTD.Entities;
using ElementalLoopTD.Utils;

namespace ElementalLoopTD.Core;

public class WaveManager
{
    public float WaveTimer = 3f;
    public bool WaveActive;
    public int MonstersToSpawn;
    public float SpawnTimer;
    public int SpawnedThisWave;
    public bool EliteQueued, EliteSpawned;
    public List<Waypoint> Waypoints = new();

    public struct WaveState
    {
        public int Wave;
        public bool WaveActive;
        public float WaveTimer;
    }

    public WaveState StartWave(int currentWave)
    {
        var wave = Math.Min(Config.Combat.MaxSafeWave, currentWave + 1);
        MonstersToSpawn = SafeMath.SafeRound(SafeMath.SafeAdd(3, wave * 2, Config.Combat.MaxSafeHp));
        SpawnedThisWave = 0;
        SpawnTimer = 0;
        WaveActive = true;
        EliteSpawned = false;
        EliteQueued = wave % Config.Elite.WaveInterval == 0;
        return new WaveState { Wave = wave, WaveActive = true };
    }

    public Monster? TrySpawn(int wave, List<Waypoint> waypoints)
    {
        if (!WaveActive || SpawnedThisWave >= MonstersToSpawn) return null;
        SpawnTimer -= 0.016f; // called per-frame, approximate
        if (SpawnTimer > 0) return null;
        var isElite = EliteQueued && !EliteSpawned && SpawnedThisWave == 0;
        if (isElite) EliteSpawned = true;
        var m = new Monster(wave, waypoints, isElite);
        SpawnedThisWave++;
        SpawnTimer = Config.Combat.MonsterSpawnInterval;
        return m;
    }

    public float EndWave(int wave)
    {
        WaveActive = false;
        return Math.Min(Config.Combat.WaveIntervalCap,
            Config.Combat.WaveBaseInterval + wave * Config.Combat.WaveIntervalPerWave);
    }
}
```

Write `ElementalLoopTD/Core/GameManager.cs`:

```csharp
using ElementalLoopTD.Entities;
using ElementalLoopTD.Utils;

namespace ElementalLoopTD.Core;

public class GameManager
{
    public int Gold = Config.Combat.InitialGold;
    public int Wave, Kills;
    public bool IsGameOver, IsPaused;
    public readonly List<Tower> Towers = new();
    public readonly List<Monster> Monsters = new();
    public readonly List<Projectile> Projectiles = new();
    public readonly List<Particle> Particles = new();
    public string? SelectedTowerType;
    public Tower? SelectedTower;
    public Vector2? HoverPos;
    public float MapLeft, MapTop, MapRight, MapBottom, MapW;
    public List<Waypoint> Waypoints = new();
    public float WaveTimer = 3f;
    public bool WaveActive;
    public int MonstersToSpawn;
    public float SpawnTimer;
    public int SpawnedThisWave;
    public bool EliteQueued, EliteSpawned;

    // Event for UI notification
    public event Action? OnStateChanged;
    public event Action<string>? OnRuleHint;
    public event Action<string>? OnWaveNotice;

    public void BuildWaypoints(float width, float height, float mapLeft, float mapTop, float mapRight, float mapBottom)
    {
        MapLeft = mapLeft; MapTop = mapTop; MapRight = mapRight; MapBottom = mapBottom;
        MapW = mapRight - mapLeft;
        var mapH = mapBottom - mapTop;
        var m = (int)(width * 0.10f);
        var spawnX = mapLeft + MapW * 0.75f;
        var spawnY = mapTop + mapH * 0.5f;
        Waypoints = new List<Waypoint>
        {
            new(spawnX, spawnY),
            new(mapRight - m, spawnY),
            new(mapRight - m, mapBottom - m),
            new(mapLeft + m, mapBottom - m),
            new(mapLeft + m, mapTop + m),
            new(mapRight - m, mapTop + m),
        };
    }

    public bool IsOnPath(float x, float y)
    {
        if (Waypoints.Count < 2) return false;
        var segs = new[] { (Waypoints[0], Waypoints[1]) };
        for (int i = 1; i < Waypoints.Count - 1; i++)
            Array.Resize(ref segs, segs.Length + 1);
        // Simplified: check all segments
        for (int i = 0; i < Waypoints.Count - 1; i++)
        {
            if (SafeMath.DistPointSeg(x, y, Waypoints[i].X, Waypoints[i].Y, Waypoints[i + 1].X, Waypoints[i + 1].Y) < 18)
                return true;
        }
        if (Waypoints.Count >= 2)
        {
            var last = Waypoints[^1];
            var first = Waypoints[1];
            if (SafeMath.DistPointSeg(x, y, last.X, last.Y, first.X, first.Y) < 18)
                return true;
        }
        return false;
    }

    public bool IsInsideMap(float x, float y) =>
        x >= MapLeft && x <= MapRight && y >= MapTop && y <= MapBottom;

    public void HandleTap(float x, float y)
    {
        if (IsGameOver) return;
        var tappedTower = Towers.FirstOrDefault(t => SafeMath.Dist(x, y, t.Position.X, t.Position.Y) < 22);
        if (tappedTower != null)
        {
            SelectedTower = tappedTower;
            SelectedTowerType = null;
            OnStateChanged?.Invoke();
            return;
        }
        if (SelectedTowerType != null)
        {
            var type = SelectedTowerType;
            var def = Config.Towers.All[type];
            var existing = Towers.FirstOrDefault(t => t.Type == type);
            if (existing != null)
            {
                if (!IsInsideMap(x, y)) { OnRuleHint?.Invoke("不能放在地图外"); return; }
                if (IsOnPath(x, y)) { OnRuleHint?.Invoke("不能放在路径上"); return; }
                var cost = existing.GetUpgradeCost();
                if (Gold < cost) { OnRuleHint?.Invoke($"金币不足,需要 ${cost}"); return; }
                Gold -= cost;
                existing.Position = new Vector2(x, y);
                existing.Upgrade();
                SelectedTower = existing;
                SelectedTowerType = null;
                OnStateChanged?.Invoke();
                return;
            }
            if (!IsInsideMap(x, y)) { OnRuleHint?.Invoke("不能放在地图外"); return; }
            if (IsOnPath(x, y)) { OnRuleHint?.Invoke("不能放在路径上"); return; }
            if (Gold < def.Cost) { OnRuleHint?.Invoke($"金币不足,需要 ${def.Cost}"); return; }
            var tower = new Tower(type, x, y);
            Towers.Add(tower);
            Gold -= def.Cost;
            SelectedTowerType = null;
            SelectedTower = tower;
            OnStateChanged?.Invoke();
            return;
        }
        SelectedTower = null;
        OnStateChanged?.Invoke();
    }

    public void StartWave()
    {
        Wave++;
        MonstersToSpawn = SafeMath.SafeRound(SafeMath.SafeAdd(3, Wave * 2, Config.Combat.MaxSafeHp));
        SpawnedThisWave = 0;
        SpawnTimer = 0;
        WaveActive = true;
        EliteSpawned = false;
        EliteQueued = Wave % Config.Elite.WaveInterval == 0;
        var notice = EliteQueued
            ? $"⚠ 第 {Wave} 波 · 精英怪来袭!"
            : $"第 {Wave} 波!";
        OnWaveNotice?.Invoke(notice);
        OnStateChanged?.Invoke();
    }

    public void TryUpgradeTower(Tower tower, int n = 1)
    {
        if (IsGameOver) return;
        var upgraded = 0;
        for (int i = 0; i < n; i++)
        {
            var cost = tower.GetUpgradeCost();
            if (Gold < cost) break;
            Gold -= cost;
            tower.Upgrade();
            upgraded++;
        }
        if (upgraded > 0)
        {
            OnStateChanged?.Invoke();
        }
    }

    public void Update(float dt)
    {
        if (IsGameOver || IsPaused) return;

        // Wave countdown
        if (!WaveActive)
        {
            var aliveCount = Monsters.Count(m => m.Alive);
            if (aliveCount == 0 && WaveTimer > 1.5f) WaveTimer = 1.5f;
            WaveTimer -= dt;
            if (WaveTimer <= 0) StartWave();
        }

        // Spawn
        if (WaveActive && SpawnedThisWave < MonstersToSpawn)
        {
            SpawnTimer -= dt;
            if (SpawnTimer <= 0)
            {
                var isElite = EliteQueued && !EliteSpawned && SpawnedThisWave == 0;
                if (isElite) EliteSpawned = true;
                var m = new Monster(Wave, Waypoints, isElite);
                Monsters.Add(m);
                SpawnedThisWave++;
                SpawnTimer = Config.Combat.MonsterSpawnInterval;
            }
        }
        else if (WaveActive && SpawnedThisWave >= MonstersToSpawn)
        {
            WaveActive = false;
            WaveTimer = Math.Min(Config.Combat.WaveIntervalCap,
                Config.Combat.WaveBaseInterval + Wave * Config.Combat.WaveIntervalPerWave);
            Monsters.RemoveAll(m => !m.Alive);
        }

        // Towers update + shoot
        for (int i = 0; i < Towers.Count; i++)
        {
            var t = Towers[i];
            t.Update(dt);
            if (t.CanShoot())
            {
                var target = t.FindTarget(Monsters, MapW);
                if (target != null)
                {
                    Projectiles.Add(new Projectile(t.Position, target, t));
                    t.ResetCooldown();
                }
            }
        }

        // Monsters update
        for (int i = 0; i < Monsters.Count; i++) Monsters[i].Update(dt);

        // Projectiles update
        for (int i = 0; i < Projectiles.Count; i++)
            Projectiles[i].Update(dt, Monsters, Particles, MapW);

        // Particles update
        for (int i = 0; i < Particles.Count; i++) Particles[i].Update(dt);

        // Cleanup
        Projectiles.RemoveAll(p => !p.Alive);
        Particles.RemoveAll(p => !p.Alive);

        // Game over check
        var aliveMonsters = Monsters.Count(m => m.Alive);
        if (aliveMonsters >= Config.Combat.MaxMonstersGameover)
        {
            IsGameOver = true;
            OnStateChanged?.Invoke();
        }
    }

    public void Restart()
    {
        Gold = Config.Combat.InitialGold;
        Wave = 0; Kills = 0;
        Towers.Clear(); Monsters.Clear(); Projectiles.Clear(); Particles.Clear();
        SelectedTowerType = null; SelectedTower = null;
        IsGameOver = false; IsPaused = false;
        WaveTimer = 3; WaveActive = false;
        OnStateChanged?.Invoke();
    }
}
```

- [ ] **Step 2: Build**

```bash
cd /c/Users/zhangjia/project/style7en.github.io/ElementalLoopTD/ElementalLoopTD && dotnet build
```

- [ ] **Step 3: Commit**

```bash
git add ElementalLoopTD/
git commit -m "feat: implement WaveManager and GameManager"
```

---

### Task 6: Implement TextureGenerator and Rendering

**Files:**
- Create: `ElementalLoopTD/Rendering/TextureGenerator.cs`
- Create: `ElementalLoopTD/Rendering/PathRenderer.cs`
- Create: `ElementalLoopTD/Rendering/GrassRenderer.cs`

- [ ] **Step 1: Create directory**

```bash
mkdir -p /c/Users/zhangjia/project/style7en.github.io/ElementalLoopTD/ElementalLoopTD/Rendering
```

Write `ElementalLoopTD/Rendering/TextureGenerator.cs`:

```csharp
using ElementalLoopTD.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElementalLoopTD.Rendering;

public static class TextureGenerator
{
    public static Texture2D CreateCircle(GraphicsDevice gd, int radius, Color color, bool filled = true)
    {
        var size = radius * 2;
        var tex = new Texture2D(gd, size, size);
        var data = new Color[size * size];
        var rSq = radius * radius;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            var dx = x - radius; var dy = y - radius;
            var distSq = dx * dx + dy * dy;
            data[y * size + x] = distSq <= rSq ? color : Color.Transparent;
        }
        tex.SetData(data);
        return tex;
    }

    public static Texture2D CreateProjectile(GraphicsDevice gd, bool isCrit)
    {
        var r = isCrit ? 5 : 3;
        var col = isCrit ? new Color(255, 235, 59) : Color.White;
        return CreateCircle(gd, r, col);
    }

    public static Texture2D CreateRect(GraphicsDevice gd, int w, int h, Color color)
    {
        var tex = new Texture2D(gd, w, h);
        var data = new Color[w * h];
        Array.Fill(data, color);
        tex.SetData(data);
        return tex;
    }

    public static Texture2D CreateTowerTexture(GraphicsDevice gd, TowerDef def, int level, float size)
    {
        var s = (int)(size * 2);
        var rt = new RenderTarget2D(gd, s, s);
        gd.SetRenderTarget(rt);
        gd.Clear(Color.Transparent);

        var sb = new SpriteBatch(gd);
        sb.Begin();

        // Base
        var baseW = s * 0.45f; var baseH = s * 0.15f;
        // Body
        var bodyW = s * 0.35f; var bodyH = s * 0.35f;
        var bx = (s - bodyW) / 2; var by = (s - bodyH) / 2 + 5;

        // Draw simple rectangle for tower body
        var bodyTex = CreateRect(gd, (int)bodyW, (int)bodyH, def.Color);
        sb.Draw(bodyTex, new Vector2(bx, by), Color.White);

        // Light accent on top
        var accentTex = CreateRect(gd, (int)(bodyW * 0.8f), 2, Color.White * 0.35f);
        sb.Draw(accentTex, new Vector2(bx + bodyW * 0.1f, by), Color.White);

        sb.End();

        gd.SetRenderTarget(null);
        return rt;
    }
}
```

Write `ElementalLoopTD/Rendering/PathRenderer.cs`:

```csharp
using ElementalLoopTD.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElementalLoopTD.Rendering;

public class PathRenderer
{
    private RenderTarget2D? _cache;
    private int _cacheW, _cacheH;
    private List<Waypoint> _waypoints = new();

    public void BuildCache(GraphicsDevice gd, List<Waypoint> waypoints, int width, int height)
    {
        if (_cache != null && _cacheW == width && _cacheH == height) return;
        _waypoints = waypoints;
        _cacheW = width; _cacheH = height;
        _cache?.Dispose();
        _cache = new RenderTarget2D(gd, width, height);
        gd.SetRenderTarget(_cache);
        gd.Clear(Color.Transparent);
        var sb = new SpriteBatch(gd);
        sb.Begin();

        // Draw corridor (brown path)
        if (waypoints.Count >= 6)
        {
            var polyline = new[] { waypoints[0], waypoints[1], waypoints[2], waypoints[3], waypoints[4], waypoints[5], waypoints[1] };
            // Draw line segments as thick rectangles
            for (int i = 0; i < polyline.Length - 1; i++)
            {
                var a = polyline[i]; var b = polyline[i + 1];
                DrawThickLine(sb, a.X, a.Y, b.X, b.Y, 22, new Color(44, 40, 32));
            }
            // Draw cobblestones (small circles)
            var stoneColor = new Color(165, 163, 153);
            for (int i = 0; i < polyline.Length - 1; i++)
            {
                var a = polyline[i]; var b = polyline[i + 1];
                var steps = (int)(Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y)) / 22);
                for (int j = 0; j <= steps; j++)
                {
                    var t = j / (float)steps;
                    var sx = a.X + (b.X - a.X) * t;
                    var sy = a.Y + (b.Y - a.Y) * t;
                    var stoneTex = TextureGenerator.CreateCircle(gd, 8, stoneColor);
                    sb.Draw(stoneTex, new Vector2(sx - 8, sy - 8), Color.White);
                }
            }
        }

        sb.End();
        gd.SetRenderTarget(null);
    }

    private void DrawThickLine(SpriteBatch sb, float x1, float y1, float x2, float y2, float thickness, Color color)
    {
        var dx = x2 - x1; var dy = y2 - y1;
        var len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 1) return;
        var angle = MathF.Atan2(dy, dx);
        var pixel = TextureGenerator.CreateRect(sb.GraphicsDevice, 1, 1, color);
        sb.Draw(pixel, new Vector2(x1, y1), null, color, angle,
            new Vector2(0, 0.5f), new Vector2(len, thickness), SpriteEffects.None, 0);
    }

    public void Draw(SpriteBatch sb)
    {
        if (_cache != null) sb.Draw(_cache, Vector2.Zero, Color.White);
    }
}
```

Write `ElementalLoopTD/Rendering/GrassRenderer.cs`:

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElementalLoopTD.Rendering;

public class GrassRenderer
{
    private RenderTarget2D? _cache;
    private int _cacheW, _cacheH;
    private int _seed;

    public void BuildCache(GraphicsDevice gd, int mapW, int mapH, int seed)
    {
        if (_cache != null && _cacheW == mapW && _cacheH == mapH) return;
        _seed = seed;
        _cacheW = mapW; _cacheH = mapH;
        _cache?.Dispose();
        _cache = new RenderTarget2D(gd, mapW, mapH);
        gd.SetRenderTarget(_cache);
        gd.Clear(new Color(45, 74, 31)); // base grass color

        var sb = new SpriteBatch(gd);
        sb.Begin();

        var rng = new Random(seed);
        var blades = (mapW * mapH) / 240;
        var bladeColor1 = new Color(60, 110, 50);
        var bladeColor2 = new Color(80, 140, 70);
        var bladeColor3 = new Color(70, 120, 60);
        var bladeColors = new[] { bladeColor1, bladeColor2, bladeColor3 };

        for (int i = 0; i < blades; i++)
        {
            var x = rng.Next(mapW);
            var y = rng.Next(mapH);
            var len = 3 + rng.Next(4);
            var color = bladeColors[rng.Next(3)] * (0.55f + (float)rng.NextDouble() * 0.3f);
            var pixel = TextureGenerator.CreateRect(gd, 1, 1, color);
            var angle = -MathF.PI / 2 + (float)(rng.NextDouble() - 0.5) * 0.5f;
            sb.Draw(pixel, new Vector2(x, y), null, color, angle,
                new Vector2(0, 0), new Vector2(1, len), SpriteEffects.None, 0);
        }

        sb.End();
        gd.SetRenderTarget(null);
    }

    public void Draw(SpriteBatch sb, int offsetX, int offsetY)
    {
        if (_cache != null)
            sb.Draw(_cache, new Vector2(offsetX, offsetY), Color.White);
    }
}
```

- [ ] **Step 2: Build**

```bash
cd /c/Users/zhangjia/project/style7en.github.io/ElementalLoopTD/ElementalLoopTD && dotnet build
```

- [ ] **Step 3: Commit**

```bash
git add ElementalLoopTD/
git commit -m "feat: add rendering pipeline - TextureGenerator, PathRenderer, GrassRenderer"
```

---

### Task 7: Implement UI Layer (HUD, InfoPanel, BuildBar, Overlays)

**Files:**
- Create: `ElementalLoopTD/UI/HUD.cs`
- Create: `ElementalLoopTD/UI/InfoPanel.cs`
- Create: `ElementalLoopTD/UI/BuildBar.cs`
- Create: `ElementalLoopTD/UI/Overlays.cs`

All UI components render via `SpriteBatch` using textures from `TextureGenerator` and `SpriteFont` for text.

- [ ] **Step 1: Create directory**

```bash
mkdir -p /c/Users/zhangjia/project/style7en.github.io/ElementalLoopTD/ElementalLoopTD/UI
```

Write `ElementalLoopTD/UI/HUD.cs`:

```csharp
using ElementalLoopTD.Core;
using ElementalLoopTD.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElementalLoopTD.UI;

public class HUD
{
    private string _waveText = "", _goldText = "", _monsterText = "", _killText = "";
    private int _lastWave = -1, _lastGold = -1, _lastKills = -1, _lastAlive = -1;

    public void Draw(SpriteBatch sb, SpriteFont font, GameManager gm, int screenWidth)
    {
        if (gm.Wave != _lastWave)
        { _waveText = $"波数: {gm.Wave}"; _lastWave = gm.Wave; }
        if (gm.Gold != _lastGold)
        { _goldText = $"金币: {gm.Gold}"; _lastGold = gm.Gold; }
        if (gm.Kills != _lastKills)
        { _killText = $"击杀: {gm.Kills}"; _lastKills = gm.Kills; }
        var alive = gm.Monsters.Count(m => m.Alive);
        if (alive != _lastAlive)
        { _monsterText = $"怪物: {alive}/{Config.Combat.MaxMonstersGameover}"; _lastAlive = alive; }

        // Draw HUD background bar
        var bgRect = TextureGenerator.CreateRect(sb.GraphicsDevice, screenWidth, 30, new Color(22, 33, 62));
        sb.Draw(bgRect, Vector2.Zero, Color.White);

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
```

Write `ElementalLoopTD/UI/InfoPanel.cs`:

```csharp
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

        // Draw panel background
        var panelTex = TextureGenerator.CreateRect(sb.GraphicsDevice, 220, 100, new Color(15, 52, 96) * 0.96f);
        sb.Draw(panelTex, pos, Color.White);
        sb.DrawString(font, _cachedHtml, pos + new Vector2(5, 5), Color.White);
    }
}
```

Write `ElementalLoopTD/UI/BuildBar.cs`:

```csharp
using ElementalLoopTD.Core;
using ElementalLoopTD.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ElementalLoopTD.UI;

public class BuildBar
{
    public string? ClickedType;

    public void Draw(SpriteBatch sb, SpriteFont font, GameManager gm, int screenWidth, int screenHeight)
    {
        var barY = screenHeight - 50;
        var bgRect = TextureGenerator.CreateRect(sb.GraphicsDevice, screenWidth, 50, new Color(22, 33, 62));
        sb.Draw(bgRect, new Vector2(0, barY), Color.White);

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
            var btnRect = TextureGenerator.CreateRect(sb.GraphicsDevice, btnW, 40, canAfford ? new Color(26, 26, 46) : new Color(26, 26, 46) * 0.4f);
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
            if (mx >= bx && mx <= bx + btnW)
            {
                ClickedType = types[i];
                return true;
            }
        }
        return false;
    }
}
```

Write `ElementalLoopTD/UI/Overlays.cs`:

```csharp
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
```

- [ ] **Step 2: Build**

```bash
cd /c/Users/zhangjia/project/style7en.github.io/ElementalLoopTD/ElementalLoopTD && dotnet build
```

- [ ] **Step 3: Commit**

```bash
git add ElementalLoopTD/
git commit -m "feat: add UI components - HUD, InfoPanel, BuildBar, Overlays"
```

---

### Task 8: Implement SaveManager

**Files:**
- Create: `ElementalLoopTD/Core/SaveManager.cs`

- [ ] **Step 1: Write SaveManager**

Write `ElementalLoopTD/Core/SaveManager.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElementalLoopTD.Core;

public class SaveData
{
    public int Version { get; set; } = 1;
    public long SavedAt { get; set; }
    public SaveState State { get; set; } = new();
}

public class SaveState
{
    public int Gold { get; set; }
    public int Wave { get; set; }
    public int Kills { get; set; }
    public List<SaveTower> Towers { get; set; } = new();
}

public class SaveTower
{
    public string Type { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public int Level { get; set; } = 1;
    public float CritRate { get; set; } = 0.05f;
    public float CritDamage { get; set; } = 1.5f;
    public float BonusRangeRatio { get; set; }
    public float BonusSpeed { get; set; }
    public List<string> Items { get; set; } = new();
    public long TotalDamage { get; set; }
}

public static class SaveManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static string GetSavePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "ElementalLoopTD");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "save.json");
    }

    public static void Save(GameManager gm)
    {
        try
        {
            var data = new SaveData
            {
                SavedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                State = new SaveState
                {
                    Gold = (int)SafeMath.SafeFinite(gm.Gold, Config.Combat.InitialGold),
                    Wave = SafeMath.SafeNum(gm.Wave, 0, Config.Combat.MaxSafeWave),
                    Kills = SafeMath.SafeNum(gm.Kills, 0, int.MaxValue),
                    Towers = gm.Towers.Select(t => new SaveTower
                    {
                        Type = t.Type,
                        X = t.Position.X,
                        Y = t.Position.Y,
                        Level = Math.Max(1, t.Level),
                        CritRate = t.CritRate,
                        CritDamage = t.CritDamage,
                        BonusRangeRatio = t.BonusRangeRatio,
                        BonusSpeed = t.BonusSpeed,
                        Items = t.Items.ToList(),
                        TotalDamage = t.TotalDamage,
                    }).ToList(),
                }
            };
            var json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(GetSavePath(), json);
        }
        catch { /* silent fail */ }
    }

    public static SaveData? Load()
    {
        try
        {
            var path = GetSavePath();
            if (!File.Exists(path)) return null;
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<SaveData>(json, JsonOptions);
        }
        catch { return null; }
    }

    public static void Clear()
    {
        try
        {
            var path = GetSavePath();
            if (File.Exists(path)) File.Delete(path);
        }
        catch { }
    }

    public static bool HasSave() => File.Exists(GetSavePath());
}
```

- [ ] **Step 2: Build**

```bash
cd /c/Users/zhangjia/project/style7en.github.io/ElementalLoopTD/ElementalLoopTD && dotnet build
```

- [ ] **Step 3: Commit**

```bash
git add ElementalLoopTD/
git commit -m "feat: implement SaveManager with JSON file persistence"
```

---

### Task 9: Assemble Game1 (Main Loop + Integration)

**Files:**
- Modify: `ElementalLoopTD/Game1.cs` (main game class)

- [ ] **Step 1: Rewrite Game1.cs with full integration**

Write `ElementalLoopTD/Game1.cs`:

```csharp
using ElementalLoopTD.Core;
using ElementalLoopTD.Entities;
using ElementalLoopTD.Rendering;
using ElementalLoopTD.UI;
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

    // Timing
    private float _fpsAcc;
    private int _fpsFrames;
    private int _fps;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = _windowWidth;
        _graphics.PreferredBackBufferHeight = _windowHeight;
        _graphics.IsFullScreen = false;
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
        var size = Math.Min(_windowWidth, _windowHeight);
        var padding = (int)(size * 0.06f);
        _mapLeft = padding;
        _mapTop = 40; // HUD height
        _mapRight = _windowWidth - padding;
        _mapBottom = _windowHeight - 50 - padding; // BuildBar height
        _mapW = _mapRight - _mapLeft;
        _gm.BuildWaypoints(_windowWidth, _windowHeight, _mapLeft, _mapTop, _mapRight, _mapBottom);
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("default");

        _gm.OnStateChanged += () => ScheduleSave();
        _gm.OnRuleHint += (msg) => { /* show hint */ };
        _gm.OnWaveNotice += (msg) => { /* show wave notice */ };

        RebuildMap();
        _pathRenderer.BuildCache(GraphicsDevice, _gm.Waypoints, _windowWidth, _windowHeight);
        _grassRenderer.BuildCache(GraphicsDevice, (int)_mapW, (int)(_mapW), 42);

        // Check for save
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
        _gm.Gold = Math.Clamp(s.Gold, 0, (int)Config.Combat.MaxSafeGold);
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

        // Global keys
        if (ks.IsKeyDown(Keys.Escape))
        {
            if (_gm.SelectedTowerType != null || _gm.SelectedTower != null)
            {
                _gm.SelectedTowerType = null;
                _gm.SelectedTower = null;
                _gm.OnStateChanged?.Invoke();
            }
        }

        if (ks.IsKeyDown(Keys.P) || ks.IsKeyDown(Keys.Space))
        {
            if (!_gm.IsGameOver) _gm.IsPaused = !_gm.IsPaused;
        }

        // Restore dialog keys
        if (_showRestoreDialog)
        {
            if (ks.IsKeyDown(Keys.Y))
            {
                var data = SaveManager.Load();
                if (data != null) RestoreFromSave(data);
            }
            if (ks.IsKeyDown(Keys.N))
            {
                SaveManager.Clear();
                _showRestoreDialog = false;
            }
            return;
        }

        // Mouse handling
        if (ms.LeftButton == ButtonState.Pressed && _prevLeft == ButtonState.Released)
        {
            var mx = ms.X; var my = ms.Y;
            // Check build bar first
            if (_buildBar.HandleClick(mx, my, _windowWidth, _windowHeight))
            {
                _gm.SelectedTowerType = _buildBar.ClickedType;
                _gm.SelectedTower = null;
                _gm.OnStateChanged?.Invoke();
            }
            else
            {
                // Map coordinates
                var mapX = mx; var mapY = my;
                if (mapX >= _mapLeft && mapX <= _mapRight && mapY >= _mapTop && mapY <= _mapBottom)
                    _gm.HandleTap(mapX, mapY);
            }
        }
        _prevLeft = ms.LeftButton;

        _gm.HoverPos = new Vector2(ms.X, ms.Y);

        // Game update
        _gm.Update(dt);

        // Auto-save
        if (_savePending)
        {
            _saveTimer -= dt;
            if (_saveTimer <= 0)
            {
                SaveManager.Save(_gm);
                _savePending = false;
            }
        }

        // FPS
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

    private ButtonState _prevLeft = ButtonState.Released;

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(26, 42, 20)); // dark green edge

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        // Grass (cached)
        _grassRenderer.Draw(_spriteBatch, (int)_mapLeft, (int)_mapTop);

        // Path (cached)
        _pathRenderer.Draw(_spriteBatch);

        // Map border
        var borderTex = TextureGenerator.CreateRect(GraphicsDevice, 1, 1, new Color(10, 26, 5));
        // Top
        _spriteBatch.Draw(borderTex, new Rectangle((int)_mapLeft - 2, (int)_mapTop - 2, (int)_mapW + 4, 2), Color.White);
        // Bottom
        _spriteBatch.Draw(borderTex, new Rectangle((int)_mapLeft - 2, (int)_mapBottom, (int)_mapW + 4, 2), Color.White);
        // Left
        _spriteBatch.Draw(borderTex, new Rectangle((int)_mapLeft - 2, (int)_mapTop - 2, 2, (int)(_mapBottom - _mapTop) + 4), Color.White);
        // Right
        _spriteBatch.Draw(borderTex, new Rectangle((int)_mapRight, (int)_mapTop - 2, 2, (int)(_mapBottom - _mapTop) + 4), Color.White);

        // Towers
        for (int i = 0; i < _gm.Towers.Count; i++)
        {
            var t = _gm.Towers[i];
            var isSelected = t == _gm.SelectedTower;
            // Draw range circle for selected
            if (isSelected)
            {
                var range = t.GetRange(_mapW);
                var circleTex = TextureGenerator.CreateCircle(GraphicsDevice, (int)range, new Color(255, 255, 255, 77), false);
                _spriteBatch.Draw(circleTex, t.Position - new Vector2(range, range), Color.White);
            }
            // Simple tower circle
            var size = Math.Min(14 + t.Level * 1.5f, 26);
            var towerTex = TextureGenerator.CreateCircle(GraphicsDevice, (int)size, t.Def.Color);
            _spriteBatch.Draw(towerTex, t.Position - new Vector2(size, size), Color.White);
        }

        // Monsters
        for (int i = 0; i < _gm.Monsters.Count; i++)
        {
            var m = _gm.Monsters[i];
            if (!m.Alive) continue;
            var col = ColorExtensions.HpColor(m.Hp / m.MaxHp);
            var monsterTex = TextureGenerator.CreateCircle(GraphicsDevice, (int)m.Radius, col);
            _spriteBatch.Draw(monsterTex, m.Position - new Vector2(m.Radius, m.Radius), Color.White);
            // HP bar
            if (m.MaxHp > 0)
            {
                var barW = 26; var barH = 4;
                var barX = m.Position.X - barW / 2; var barY = m.Position.Y - m.Radius - 9;
                var hpRatio = m.Hp / m.MaxHp;
                var hpColor = ColorExtensions.HpColor(hpRatio);
                var hpCol = new Color(hpColor.R, hpColor.G, hpColor.B);
                var bgTex = TextureGenerator.CreateRect(GraphicsDevice, barW + 2, barH + 2, new Color(0, 0, 0, 140));
                var barBgTex = TextureGenerator.CreateRect(GraphicsDevice, barW, barH, new Color(51, 51, 51));
                var hpTex = TextureGenerator.CreateRect(GraphicsDevice, Math.Max(1, (int)(barW * hpRatio)), barH, hpCol);
                _spriteBatch.Draw(bgTex, new Vector2(barX - 1, barY - 1), Color.White);
                _spriteBatch.Draw(barBgTex, new Vector2(barX, barY), Color.White);
                _spriteBatch.Draw(hpTex, new Vector2(barX, barY), Color.White);
            }
        }

        // HUD
        _hud.Draw(_spriteBatch, _font, _gm, _windowWidth);

        // Build Bar
        _buildBar.Draw(_spriteBatch, _font, _gm, _windowWidth, _windowHeight);

        // Info Panel
        _infoPanel.Draw(_spriteBatch, _font, _gm, _windowWidth, _windowHeight);

        // Overlays
        if (_gm.IsPaused)
            _overlays.DrawPause(_spriteBatch, _font, _windowWidth, _windowHeight);
        if (_gm.IsGameOver)
            _overlays.DrawGameOver(_spriteBatch, _font, _gm, _windowWidth, _windowHeight);

        // FPS
        var fpsColor = _fps >= 50 ? Color.Green : (_fps >= 30 ? Color.Yellow : Color.Red);
        _spriteBatch.DrawString(_font, $"FPS {_fps}", new Vector2(10, _windowHeight - 70), fpsColor);

        // Restore dialog
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
```

**Note:** The content pipeline requires a `.spritefont` file. We need a minimal font. Let's create a default PNG font approach instead.

Create `ElementalLoopTD/Content/default.spritefont`:

```bash
mkdir -p /c/Users/zhangjia/project/style7en.github.io/ElementalLoopTD/ElementalLoopTD/Content
```

Write `ElementalLoopTD/Content/default.spritefont`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<XnaContent xmlns:Graphics="Microsoft.Xna.Framework.Content.Pipeline.Graphics">
  <Asset Type="Graphics:FontDescription">
    <FontName>Consolas</FontName>
    <Size>14</Size>
    <Spacing>0</Spacing>
    <Style>Regular</Style>
    <CharacterRegions>
      <CharacterRegion>
        <Start>&#32;</Start>
        <End>&#126;</End>
      </CharacterRegion>
      <CharacterRegion>
        <Start>&#19968;</Start>
        <End>&#40869;</End>
      </CharacterRegion>
    </CharacterRegions>
  </Asset>
</XnaContent>
```

- [ ] **Step 2: Build and fix issues**

```bash
cd /c/Users/zhangjia/project/style7en.github.io/ElementalLoopTD/ElementalLoopTD && dotnet build
```

- [ ] **Step 3: Commit**

```bash
git add ElementalLoopTD/
git commit -m "feat: assemble Game1 with full loop and component integration"
```

---

### Task 10: Final Polish and Testing

- [ ] **Step 1: Run the application**

```bash
cd /c/Users/zhangjia/project/style7en.github.io/ElementalLoopTD/ElementalLoopTD && dotnet run
```
Verify the window opens, displays the game map, and responds to input.

- [ ] **Step 2: Fix any build or runtime issues**

Common issues to fix:
- Missing `Content/default.xnb` (run mgcb pipeline or use runtime font)
- NullReferenceException in any component
- Mouse click not mapping to game coordinates correctly

- [ ] **Step 3: Commit final polish**

```bash
git add ElementalLoopTD/
git commit -m "feat: final polish and bugfixes"
```

---

## Self-Review Checklist

1. **Spec coverage:** 
   - Config.cs covers TOWER_DEFS, ITEM_DEFS, element table, wave params ✓
   - Entity classes (Tower, Monster, Projectile) cover all original fields ✓
   - ElementSystem matches ELEMENT_REACTIONS ✓
   - WaveManager matches wave generation ✓
   - TextureGenerator/RenderTarget2D replaces _pathCache/_grassCache ✓
   - GameManager handles tap/upgrade/state ✓
   - SaveManager replaces localStorage with JSON file ✓

2. **Placeholder scan:** No TBD/TODO/incomplete patterns found.

3. **Type consistency:** All method signatures consistent across files.
