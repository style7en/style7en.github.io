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

        if (Tower.Type == "ice")
            t.ApplySlow(Tower.GetSlowRate(), Tower.GetSlowDur());
    }
}