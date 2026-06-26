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