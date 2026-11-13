using System.Collections.Generic;

namespace Game.Scenarios;

public sealed class ThemeDefinition
{
    public string BoardImage { get; init; } = string.Empty;
    public string FleetImage { get; init; } = string.Empty;
    public Dictionary<string, string> Colors { get; init; } = [];
    public Dictionary<string, string> Audio { get; init; } = [];
}
