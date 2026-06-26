using Microsoft.Xna.Framework;

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