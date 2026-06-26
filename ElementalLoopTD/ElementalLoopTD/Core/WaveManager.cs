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
        SpawnTimer -= 0.016f;
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