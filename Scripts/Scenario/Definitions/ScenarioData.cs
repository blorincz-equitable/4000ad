namespace Game.Scenarios;

public sealed class ScenarioData
{
    public required ScenarioDefinition Scenario { get; init; }
    public required GalaxyDefinition Galaxy { get; init; }
    public required PlayersDefinition Players { get; init; }
    public required RulesDefinition Rules { get; init; }
    public required ThemeDefinition Theme { get; init; }
}
