using System.Collections.Generic;

namespace Game.Scenarios;

public sealed class GalaxyDefinition
{
    public List<SectorDefinition> Sectors { get; init; } = [];
    public List<StarTypeDefinition> StarTypes { get; init; } = [];
    public List<StarDefinition> Stars { get; init; } = [];
    public List<WarpRouteDefinition> WarpRoutes { get; init; } = [];
}
