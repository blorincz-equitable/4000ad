namespace Game.Scenarios;

public sealed class ScenarioDefinition
{
    public int SchemaVersion { get; init; }
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string GalaxyFile { get; init; } = string.Empty;
    public string PlayersFile { get; init; } = string.Empty;
    public string RulesFile { get; init; } = string.Empty;
    public string ThemeFile { get; init; } = string.Empty;
}
