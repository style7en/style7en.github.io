using ElementalLoopTD.Entities;
using ElementalLoopTD.Utils;
using Microsoft.Xna.Framework;

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
        for (int i = 0; i < Waypoints.Count - 1; i++)
        {
            if (SafeMath.DistPointSeg(x, y,
                Waypoints[i].X, Waypoints[i].Y,
                Waypoints[i + 1].X, Waypoints[i + 1].Y) < 18)
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
            OnStateChanged?.Invoke();
    }

    public void Update(float dt)
    {
        if (IsGameOver || IsPaused) return;

        if (!WaveActive)
        {
            var aliveCount = Monsters.Count(m => m.Alive);
            if (aliveCount == 0 && WaveTimer > 1.5f) WaveTimer = 1.5f;
            WaveTimer -= dt;
            if (WaveTimer <= 0) StartWave();
        }

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

        for (int i = 0; i < Monsters.Count; i++)
            Monsters[i].Update(dt);

        for (int i = 0; i < Projectiles.Count; i++)
            Projectiles[i].Update(dt, Monsters, Particles, MapW);

        for (int i = 0; i < Particles.Count; i++)
            Particles[i].Update(dt);

        Projectiles.RemoveAll(p => !p.Alive);
        Particles.RemoveAll(p => !p.Alive);

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