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