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

        WriteJson(Path.Combine(directory, definition.GalaxyFile), data.Galaxy);
        WriteJson(Path.Combine(directory, definition.PlayersFile), data.Players);
        WriteJson(Path.Combine(directory, definition.RulesFile), data.Rules);
        WriteJson(Path.Combine(directory, definition.ThemeFile), data.Theme);
        WriteJson(absoluteScenarioPath, definition);
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
}
