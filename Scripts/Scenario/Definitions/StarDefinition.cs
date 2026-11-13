namespace Game.Scenarios;

public sealed class StarDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string SectorId { get; init; } = string.Empty;
    public string StarTypeId { get; init; } = string.Empty;
    public PositionDefinition Position { get; init; } = new();
}

public sealed class PositionDefinition
{
    public float X { get; init; }
    public float Y { get; init; }
}
