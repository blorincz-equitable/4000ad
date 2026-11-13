using System;
using System.Globalization;
using Game.Scenarios;
using Godot;

namespace Game.ScenarioEditor;

public partial class GalaxyCanvas : Control
{
    public GalaxyDefinition Galaxy { get; set; } = new();
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

        DrawRect(new Rect2(Vector2.Zero, Size), ParseColor(Galaxy.BackgroundColor, Colors.MidnightBlue));

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
            DrawCircle(position, radius, ParseColor(starType.DisplayColor, Colors.White), true, -1, true);

            DrawCenteredStarName(star.Name.ToUpperInvariant(), position, diameter);
        }
    }

    private void DrawCenteredStarName(string name, Vector2 center, float diameter)
    {
        var font = ThemeDB.FallbackFont;
        var fontSize = Math.Max(1, Mathf.RoundToInt(diameter * 0.4f));
        var textSize = font.GetStringSize(name, HorizontalAlignment.Left, -1, fontSize);
        while (fontSize > 1 && textSize.X > diameter * 0.85f)
        {
            fontSize--;
            textSize = font.GetStringSize(name, HorizontalAlignment.Left, -1, fontSize);
        }

        var baselineY = center.Y + (font.GetAscent(fontSize) - font.GetDescent(fontSize)) / 2;
        var position = new Vector2(center.X - textSize.X / 2, baselineY);
        DrawString(font, position, name, HorizontalAlignment.Left, -1, fontSize, Colors.Black);
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
