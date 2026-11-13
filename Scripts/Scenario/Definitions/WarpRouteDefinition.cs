namespace Game.Scenarios;

public sealed class WarpRouteDefinition
{
    public string Id { get; init; } = string.Empty;
    public string FromStarId { get; init; } = string.Empty;
    public string ToStarId { get; init; } = string.Empty;
    public float Distance { get; init; }
}
