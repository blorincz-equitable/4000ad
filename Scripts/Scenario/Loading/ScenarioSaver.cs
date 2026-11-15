using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace Game.Scenarios;

public sealed class ScenarioSaver
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public void Save(string scenarioFilePath, ScenarioData data)
    {
        Save(scenarioFilePath, data, null);
    }

    public void Save(string scenarioFilePath, ScenarioData data, string backgroundImageSourcePath)
    {
        var errors = ScenarioValidator.Validate(data);
        if (errors.Count > 0)
        {
            throw new InvalidDataException(
                $"Scenario '{data.Scenario.Id}' is invalid:{System.Environment.NewLine}- {string.Join($"{System.Environment.NewLine}- ", errors)}");
        }

        var absoluteScenarioPath = ToAbsolutePath(scenarioFilePath);
        var directory = Path.GetDirectoryName(absoluteScenarioPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException("The scenario file path must include a directory.", nameof(scenarioFilePath));
        }

        Directory.CreateDirectory(directory);

        var fileStem = Path.GetFileNameWithoutExtension(absoluteScenarioPath);
        var definition = new ScenarioDefinition
        {
            SchemaVersion = data.Scenario.SchemaVersion,
            Id = data.Scenario.Id,
            Name = data.Scenario.Name,
            GalaxyFile = $"{fileStem}.galaxy.json",
            PlayersFile = $"{fileStem}.players.json",
            RulesFile = $"{fileStem}.rules.json",
            ThemeFile = $"{fileStem}.theme.json"
        };

        var galaxyToSave = data.Galaxy;
        var backgroundImageFile = data.Galaxy.BackgroundImageFile;
        if (backgroundImageSourcePath != null)
        {
            if (!string.IsNullOrWhiteSpace(backgroundImageSourcePath))
            {
                var absoluteImageSourcePath = Path.GetFullPath(backgroundImageSourcePath);
                var imageExtension = Path.GetExtension(absoluteImageSourcePath);
                if (string.IsNullOrWhiteSpace(imageExtension))
                {
                    throw new ArgumentException("The background image file must have an extension.", nameof(backgroundImageSourcePath));
                }

                var imageFileName = $"{fileStem}.background{imageExtension}";
                var absoluteImageTargetPath = Path.Combine(directory, imageFileName);
                if (!string.Equals(absoluteImageSourcePath, absoluteImageTargetPath, StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(absoluteImageSourcePath, absoluteImageTargetPath, overwrite: true);
                }

                backgroundImageFile = imageFileName;
            }
            else
            {
                backgroundImageFile = string.Empty;
            }

            galaxyToSave = CopyGalaxyWithBackgroundImageFile(data.Galaxy, backgroundImageFile);
        }

        WriteJson(Path.Combine(directory, definition.GalaxyFile), galaxyToSave);
        WriteJson(Path.Combine(directory, definition.PlayersFile), data.Players);
        WriteJson(Path.Combine(directory, definition.RulesFile), data.Rules);
        WriteJson(Path.Combine(directory, definition.ThemeFile), data.Theme);
        WriteJson(absoluteScenarioPath, definition);
        if (backgroundImageSourcePath != null)
        {
            data.Galaxy.BackgroundImageFile = backgroundImageFile;
        }
    }

    private static string ToAbsolutePath(string path)
    {
        if (path.StartsWith("res://", StringComparison.Ordinal)
            || path.StartsWith("user://", StringComparison.Ordinal))
        {
            return ProjectSettings.GlobalizePath(path);
        }

        return Path.GetFullPath(path);
    }

    private static void WriteJson<T>(string path, T data)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(data, JsonOptions));
    }

    private static GalaxyDefinition CopyGalaxyWithBackgroundImageFile(
        GalaxyDefinition galaxy,
        string backgroundImageFile)
    {
        return new GalaxyDefinition
        {
            Rows = galaxy.Rows,
            Columns = galaxy.Columns,
            BackgroundColor = galaxy.BackgroundColor,
            BackgroundImageFile = backgroundImageFile,
            SectorLineColor = galaxy.SectorLineColor,
            SectorLineWidth = galaxy.SectorLineWidth,
            Sectors = galaxy.Sectors,
            StarTypes = galaxy.StarTypes,
            Stars = galaxy.Stars,
            WarpRoutes = galaxy.WarpRoutes
        };
    }
}
