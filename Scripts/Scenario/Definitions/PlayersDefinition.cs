using System.Collections.Generic;

namespace Game.Scenarios;

public sealed class PlayersDefinition
{
    public List<PlayerDefinition> Players { get; init; } = [];
    public List<StartingAssetDefinition> StartingAssets { get; init; } = [];
}

public sealed class PlayerDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
    public bool IsAi { get; init; }
}

public sealed class StartingAssetDefinition
{
    public string PlayerId { get; init; } = string.Empty;
    public string StarId { get; init; } = string.Empty;
    public int ShipCount { get; init; }
}
