using System;
using System.Globalization;
using Game.Scenarios;
using Godot;

namespace Game.ScenarioEditor;

public partial class GalaxyCanvas : Control
{
    private const int SectorNameFontSize = 12;
    private const int StarSectorNameFontSize = 9;
    private const float StarAttributeSymbolSize = 4;
    private const float StarAttributeStrokeWidth = 1.5f;

    public GalaxyDefinition Galaxy { get; set; } = new();
    public Texture2D BackgroundTexture { get; set; }
    public int SelectedRow { get; set; } = -1;
    public int SelectedColumn { get; set; } = -1;

    public event Action<int, int> SectorSelected;
    public event Action<string> StarSelected;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
    }

    public override void _Draw()
    {
        var rows = Math.Max(1, Galaxy.Rows);
        var columns = Math.Max(1, Galaxy.Columns);
        var gridRect = GetGridRect();
        var cellSize = gridRect.Size.X / columns;

        DrawRect(new Rect2(Vector2.Zero, Size), Colors.MidnightBlue);
        DrawRect(gridRect, ParseColor(Galaxy.BackgroundColor, Colors.MidnightBlue));
        if (BackgroundTexture != null)
        {
            DrawTextureRect(BackgroundTexture, gridRect, false);
        }

        var lineColor = ParseColor(Galaxy.SectorLineColor, Colors.Black);
        var lineWidth = Math.Max(1, Galaxy.SectorLineWidth);
        for (var column = 1; column < columns; column++)
        {
            var x = gridRect.Position.X + cellSize * column;
            DrawLine(new Vector2(x, gridRect.Position.Y), new Vector2(x, gridRect.End.Y), lineColor, lineWidth);
        }

        for (var row = 1; row < rows; row++)
        {
            var y = gridRect.Position.Y + cellSize * row;
            DrawLine(new Vector2(gridRect.Position.X, y), new Vector2(gridRect.End.X, y), lineColor, lineWidth);
        }

        DrawRect(gridRect, lineColor, false, lineWidth);

        if (SelectedRow >= 0 && SelectedRow < rows && SelectedColumn >= 0 && SelectedColumn < columns)
        {
            var selectedRect = new Rect2(
                gridRect.Position.X + SelectedColumn * cellSize + 3,
                gridRect.Position.Y + SelectedRow * cellSize + 3,
                cellSize - 6,
                cellSize - 6);
            DrawRect(selectedRect, Colors.White, false, 2);
        }

        DrawStars(gridRect.Position, cellSize);
        DrawSectorNames(gridRect.Position, cellSize);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseEvent
            || !mouseEvent.Pressed
            || mouseEvent.ButtonIndex != MouseButton.Left
            || Size.X <= 0
            || Size.Y <= 0
            || Galaxy.Columns <= 0
            || Galaxy.Rows <= 0)
        {
            return;
        }

        var gridRect = GetGridRect();
        if (!gridRect.HasPoint(mouseEvent.Position))
        {
            return;
        }

        var cellSize = gridRect.Size.X / Galaxy.Columns;
        var column = Math.Clamp((int)((mouseEvent.Position.X - gridRect.Position.X) / cellSize), 0, Galaxy.Columns - 1);
        var row = Math.Clamp((int)((mouseEvent.Position.Y - gridRect.Position.Y) / cellSize), 0, Galaxy.Rows - 1);
        SectorSelected?.Invoke(row, column);

        var clickedStar = FindStarAt(mouseEvent.Position, gridRect.Position, cellSize);
        if (clickedStar != null)
        {
            StarSelected?.Invoke(clickedStar.Id);
        }

        AcceptEvent();
    }

    private Rect2 GetGridRect()
    {
        var rows = Math.Max(1, Galaxy.Rows);
        var columns = Math.Max(1, Galaxy.Columns);
        var cellSize = Math.Min(Size.X / columns, Size.Y / rows);
        var gridSize = new Vector2(cellSize * columns, cellSize * rows);
        return new Rect2((Size - gridSize) / 2, gridSize);
    }

    private void DrawStars(Vector2 gridOrigin, float cellSize)
    {
        foreach (var star in Galaxy.Stars)
        {
            var sector = Galaxy.Sectors.Find(candidate => candidate.Id == star.SectorId);
            var starType = Galaxy.StarTypes.Find(candidate => candidate.Id == star.StarTypeId);
            if (sector == null || starType == null)
            {
                continue;
            }

            var position = GetStarPosition(star, sector, starType, gridOrigin, cellSize);
            var diameter = GetStarDiameter(starType.Size, cellSize);
            var radius = diameter / 2;
            if (star.IsHomeStar)
            {
                var haloOffset = Math.Max(2, cellSize * 0.012f);
                var haloWidth = Math.Max(1, cellSize * 0.008f);
                DrawCircle(position, radius + haloOffset, Colors.Gold, false, haloWidth, true);
            }

            DrawCircle(position, radius, ParseColor(starType.DisplayColor, Colors.White), true, -1, true);

            var fontSizePercent = starType.Size switch
            {
                StarSize.Small => Galaxy.SmallStarFontSizePercent,
                StarSize.Medium => Galaxy.MediumStarFontSizePercent,
                StarSize.Large => Galaxy.LargeStarFontSizePercent,
                _ => Galaxy.MediumStarFontSizePercent
            };
            var fontSize = Math.Max(1, Mathf.RoundToInt(diameter * fontSizePercent / 100.0f));
            DrawCenteredStarName(star.Name.ToUpperInvariant(), position + new Vector2(0, 1), fontSize);
            DrawSectorNameInStar(sector.Name, position, diameter);
            DrawStarAttributes(star, position, diameter);
        }
    }

    private void DrawSectorNames(Vector2 gridOrigin, float cellSize)
    {
        var font = ThemeDB.FallbackFont;
        var lineColor = ParseColor(Galaxy.SectorLineColor, Colors.Black);
        foreach (var sector in Galaxy.Sectors)
        {
            if (string.IsNullOrWhiteSpace(sector.Name))
            {
                continue;
            }

            var name = sector.Name.ToUpperInvariant();
            var textWidth = font.GetStringSize(name, HorizontalAlignment.Left, -1, SectorNameFontSize).X;
            var centerX = gridOrigin.X + (sector.Column + 0.5f) * cellSize;
            var baselineY = gridOrigin.Y + sector.Row * cellSize + cellSize * 0.025f
                + font.GetAscent(SectorNameFontSize);
            var position = new Vector2(centerX - textWidth / 2, baselineY);
            DrawString(font, position, name, HorizontalAlignment.Left, -1, SectorNameFontSize, lineColor);
        }
    }

    private void DrawSectorNameInStar(string sectorName, Vector2 center, float diameter)
    {
        if (string.IsNullOrWhiteSpace(sectorName))
        {
            return;
        }

        var name = sectorName.ToUpperInvariant();
        var font = ThemeDB.FallbackFont;
        var textWidth = font.GetStringSize(name, HorizontalAlignment.Left, -1, StarSectorNameFontSize).X;
        var lineColor = ParseColor(Galaxy.SectorLineColor, Colors.Black);
        var baselineY = center.Y - diameter / 2 + diameter * 0.02f + font.GetAscent(StarSectorNameFontSize);
        DrawString(
            font,
            new Vector2(center.X - textWidth / 2, baselineY),
            name,
            HorizontalAlignment.Left,
            -1,
            StarSectorNameFontSize,
            lineColor);
    }

    private void DrawStarAttributes(StarDefinition star, Vector2 center, float diameter)
    {
        if (!star.HasPopulation && !star.HasResources)
        {
            return;
        }

        var symbolSize = StarAttributeSymbolSize;
        const float gap = 2;
        var strokeWidth = StarAttributeStrokeWidth;
        var markerY = center.Y + diameter * 0.32f;
        var markerCount = (star.HasPopulation ? 1 : 0) + (star.HasResources ? 1 : 0);
        var totalWidth = markerCount * symbolSize + (markerCount - 1) * gap;
        var markerCenterX = center.X - totalWidth / 2 + symbolSize / 2;

        if (star.HasPopulation)
        {
            DrawCircle(
                new Vector2(markerCenterX, markerY),
                (symbolSize - strokeWidth) / 2,
                Colors.Black,
                false,
                strokeWidth,
                true);
            markerCenterX += symbolSize + gap;
        }

        if (star.HasResources)
        {
            DrawLine(
                new Vector2(markerCenterX - symbolSize / 2, markerY),
                new Vector2(markerCenterX + symbolSize / 2, markerY),
                Colors.Black,
                strokeWidth);
            DrawLine(
                new Vector2(markerCenterX, markerY - symbolSize / 2),
                new Vector2(markerCenterX, markerY + symbolSize / 2),
                Colors.Black,
                strokeWidth);
        }
    }

    private void DrawCenteredStarName(string name, Vector2 center, int fontSize)
    {
        var font = ThemeDB.FallbackFont;
        var firstSpace = name.IndexOf(' ');
        var secondLine = firstSpace < 0 ? string.Empty : name[(firstSpace + 1)..].Trim();
        var lines = string.IsNullOrEmpty(secondLine)
            ? new[] { name }
            : new[] { name[..firstSpace], secondLine };
        var lineAdvance = Math.Max(1, fontSize * 0.9f);
        var baselineY = center.Y + (font.GetAscent(fontSize) - font.GetDescent(fontSize)) / 2;
        baselineY -= lineAdvance * (lines.Length - 1) / 2;
        for (var index = 0; index < lines.Length; index++)
        {
            var textWidth = font.GetStringSize(lines[index], HorizontalAlignment.Left, -1, fontSize).X;
            var position = new Vector2(center.X - textWidth / 2, baselineY + lineAdvance * index);
            DrawString(font, position, lines[index], HorizontalAlignment.Left, -1, fontSize, Colors.Black);
        }
    }

    private StarDefinition FindStarAt(Vector2 position, Vector2 gridOrigin, float cellSize)
    {
        StarDefinition nearestStar = null;
        var nearestDistanceSquared = float.MaxValue;

        foreach (var star in Galaxy.Stars)
        {
            var sector = Galaxy.Sectors.Find(candidate => candidate.Id == star.SectorId);
            var starType = Galaxy.StarTypes.Find(candidate => candidate.Id == star.StarTypeId);
            if (sector == null || starType == null)
            {
                continue;
            }

            var starPosition = GetStarPosition(star, sector, starType, gridOrigin, cellSize);
            var distanceSquared = position.DistanceSquaredTo(starPosition);
            var hitRadius = GetStarDiameter(starType.Size, cellSize) / 2 + cellSize * 0.015f;
            if (distanceSquared <= hitRadius * hitRadius && distanceSquared < nearestDistanceSquared)
            {
                nearestStar = star;
                nearestDistanceSquared = distanceSquared;
            }
        }

        return nearestStar;
    }

    private static Vector2 GetStarPosition(
        StarDefinition star,
        SectorDefinition sector,
        StarTypeDefinition starType,
        Vector2 gridOrigin,
        float cellSize)
    {
        var diameter = GetStarDiameter(starType.Size, cellSize);
        var inset = diameter / 2 + cellSize * 0.02f;
        var availableSize = Math.Max(0, cellSize - inset * 2);
        return new Vector2(
            gridOrigin.X + sector.Column * cellSize + inset + availableSize * star.Position.X,
            gridOrigin.Y + sector.Row * cellSize + inset + availableSize * star.Position.Y);
    }

    private static float GetStarDiameter(StarSize size, float sectorSize)
    {
        var fraction = size switch
        {
            StarSize.Small => 0.12f,
            StarSize.Medium => 0.16f,
            StarSize.Large => 0.20f,
            _ => 0.16f
        };
        return sectorSize * fraction;
    }

    private static Color ParseColor(string value, Color fallback)
    {
        if (value.Length == 7
            && value[0] == '#'
            && uint.TryParse(value.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255.0f,
                ((rgb >> 8) & 0xFF) / 255.0f,
                (rgb & 0xFF) / 255.0f);
        }

        return fallback;
    }
}
