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

        var reactionMul = 1f;
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

        var cleanDmg = Math.Clamp(rawDmg, 0, (int)Config.Combat.MaxSafeAtk);
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