using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Scenarios;

public static class ScenarioValidator
{
    public static List<string> Validate(ScenarioData data)
    {
        var errors = new List<string>();
        var scenario = data.Scenario;
        var galaxy = data.Galaxy;
        var players = data.Players;

        RequireValue(scenario.Id, "scenario.id", errors);
        RequireValue(scenario.Name, "scenario.name", errors);
        if (scenario.SchemaVersion < 1)
        {
            errors.Add("scenario.schemaVersion must be greater than zero.");
        }

        if (galaxy.Rows < 1 || galaxy.Columns < 1)
        {
            errors.Add("Galaxy rows and columns must both be greater than zero.");
        }

        if (players.Players.Count is < 2 or > 4)
        {
            errors.Add("A scenario must define between two and four players.");
        }

        CheckUniqueIds(galaxy.Sectors.Select(sector => sector.Id), "sector", errors);
        CheckUniqueIds(galaxy.StarTypes.Select(starType => starType.Id), "star type", errors);
        CheckUniqueIds(galaxy.Stars.Select(star => star.Id), "star", errors);
        CheckUniqueIds(galaxy.WarpRoutes.Select(route => route.Id), "warp route", errors);
        CheckUniqueIds(players.Players.Select(player => player.Id), "player", errors);

        foreach (var starType in galaxy.StarTypes)
        {
            RequireValue(starType.Name, $"starType '{starType.Id}'.name", errors);
            RequireValue(starType.DisplayColor, $"starType '{starType.Id}'.displayColor", errors);
        }

        foreach (var player in players.Players)
        {
            RequireValue(player.Name, $"player '{player.Id}'.name", errors);
        }

        var sectorIds = galaxy.Sectors.Select(sector => sector.Id).ToHashSet(StringComparer.Ordinal);
        var starTypeIds = galaxy.StarTypes.Select(starType => starType.Id).ToHashSet(StringComparer.Ordinal);
        var starIds = galaxy.Stars.Select(star => star.Id).ToHashSet(StringComparer.Ordinal);
        var playerIds = players.Players.Select(player => player.Id).ToHashSet(StringComparer.Ordinal);

        var sectorCoordinates = new HashSet<(int Row, int Column)>();
        foreach (var sector in galaxy.Sectors)
        {
            if (sector.Row < 0 || sector.Row >= galaxy.Rows || sector.Column < 0 || sector.Column >= galaxy.Columns)
            {
                errors.Add($"Sector '{sector.Id}' is outside the configured galaxy grid.");
            }

            if (!sectorCoordinates.Add((sector.Row, sector.Column)))
            {
                errors.Add($"Multiple sectors are assigned to row {sector.Row}, column {sector.Column}.");
            }
        }

        if (sectorCoordinates.Count != galaxy.Rows * galaxy.Columns)
        {
            errors.Add("The number of sectors must match the configured galaxy grid.");
        }

        foreach (var star in galaxy.Stars)
        {
            RequireValue(star.Name, $"star '{star.Id}'.name", errors);

            if (!sectorIds.Contains(star.SectorId))
            {
                errors.Add($"Star '{star.Id}' references unknown sector '{star.SectorId}'.");
            }

            if (!starTypeIds.Contains(star.StarTypeId))
            {
                errors.Add($"Star '{star.Id}' references unknown star type '{star.StarTypeId}'.");
            }

            if (star.Position.X is < 0 or > 1 || star.Position.Y is < 0 or > 1)
            {
                errors.Add($"Star '{star.Id}' position must be within its sector (x and y from zero to one).");
            }
        }

        foreach (var route in galaxy.WarpRoutes)
        {
            if (!starIds.Contains(route.FromStarId))
            {
                errors.Add($"Warp route '{route.Id}' references unknown origin star '{route.FromStarId}'.");
            }

            if (!starIds.Contains(route.ToStarId))
            {
                errors.Add($"Warp route '{route.Id}' references unknown destination star '{route.ToStarId}'.");
            }

            if (route.FromStarId == route.ToStarId)
            {
                errors.Add($"Warp route '{route.Id}' must connect two different stars.");
            }

            if (route.Distance <= 0)
            {
                errors.Add($"Warp route '{route.Id}' must have a positive distance.");
            }
        }

        var startingAssetKeys = new HashSet<(string PlayerId, string StarId)>();
        foreach (var asset in players.StartingAssets)
        {
            if (!playerIds.Contains(asset.PlayerId))
            {
                errors.Add($"Starting asset references unknown player '{asset.PlayerId}'.");
            }

            if (!starIds.Contains(asset.StarId))
            {
                errors.Add($"Starting asset references unknown star '{asset.StarId}'.");
            }

            if (asset.ShipCount < 0)
            {
                errors.Add($"Starting asset for player '{asset.PlayerId}' at star '{asset.StarId}' has a negative ship count.");
            }

            if (!startingAssetKeys.Add((asset.PlayerId, asset.StarId)))
            {
                errors.Add($"Player '{asset.PlayerId}' has multiple starting asset entries for star '{asset.StarId}'.");
            }
        }

        ValidateRule(data.Rules.Combat, "rules.combat", errors);
        ValidateRule(data.Rules.Production, "rules.production", errors);
        ValidateRule(data.Rules.Movement, "rules.movement", errors);
        ValidateRule(data.Rules.Victory, "rules.victory", errors);

        return errors;
    }

    private static void CheckUniqueIds(IEnumerable<string> ids, string itemName, List<string> errors)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in ids)
        {
            RequireValue(id, $"{itemName}.id", errors);
            if (!seen.Add(id))
            {
                errors.Add($"Duplicate {itemName} id '{id}'.");
            }
        }
    }

    private static void ValidateRule(RuleDefinition rule, string path, List<string> errors)
    {
        RequireValue(rule.Id, $"{path}.id", errors);
        RequireValue(rule.Type, $"{path}.type", errors);
    }

    private static void RequireValue(string value, string path, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{path} must not be empty.");
        }
    }
}
