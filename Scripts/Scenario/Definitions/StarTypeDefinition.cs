namespace Game.Scenarios;

public sealed class StarTypeDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string DisplayColor { get; init; } = string.Empty;
    public int ProductionValue { get; init; }
}
