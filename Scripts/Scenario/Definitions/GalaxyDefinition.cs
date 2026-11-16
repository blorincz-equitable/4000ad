using System.Collections.Generic;

namespace Game.Scenarios;

public sealed class GalaxyDefinition
{
    public int Rows { get; set; } = 1;
    public int Columns { get; set; } = 1;
    public string BackgroundColor { get; set; } = "#191970";
    public string BackgroundImageFile { get; set; } = string.Empty;
    public int SmallStarFontSizePercent { get; set; } = 20;
    public int MediumStarFontSizePercent { get; set; } = 20;
    public int LargeStarFontSizePercent { get; set; } = 20;
    public string SectorLineColor { get; set; } = "#000000";
    public float SectorLineWidth { get; set; } = 2;
    public List<SectorDefinition> Sectors { get; init; } = [];
    public List<StarTypeDefinition> StarTypes { get; init; } = [];
    public List<StarDefinition> Stars { get; init; } = [];
    public List<WarpRouteDefinition> WarpRoutes { get; init; } = [];
}
