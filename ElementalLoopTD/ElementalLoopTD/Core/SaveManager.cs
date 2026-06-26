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
                    Gold = Math.Max(0, gm.Gold),
                    Wave = Math.Clamp(gm.Wave, 0, Config.Combat.MaxSafeWave),
                    Kills = Math.Max(0, gm.Kills),
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
