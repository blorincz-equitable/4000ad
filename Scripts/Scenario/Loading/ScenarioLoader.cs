using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace Game.Scenarios;

public sealed class ScenarioLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public ScenarioData Load(string scenarioFilePath)
    {
        var scenario = ReadJson<ScenarioDefinition>(scenarioFilePath);
        var separatorIndex = Math.Max(scenarioFilePath.LastIndexOf('/'), scenarioFilePath.LastIndexOf('\\'));
        var directory = separatorIndex < 0 ? string.Empty : scenarioFilePath[..(separatorIndex + 1)];

        var data = new ScenarioData
        {
            Scenario = scenario,
            Galaxy = ReadJson<GalaxyDefinition>(directory + scenario.GalaxyFile),
            Players = ReadJson<PlayersDefinition>(directory + scenario.PlayersFile),
            Rules = ReadJson<RulesDefinition>(directory + scenario.RulesFile),
            Theme = ReadJson<ThemeDefinition>(directory + scenario.ThemeFile)
        };

        var errors = ScenarioValidator.Validate(data);
        if (errors.Count > 0)
        {
            throw new InvalidDataException(
                $"Scenario '{scenario.Id}' is invalid:{System.Environment.NewLine}- {string.Join($"{System.Environment.NewLine}- ", errors)}");
        }

        return data;
    }

    private static T ReadJson<T>(string resourcePath) where T : class
    {
        if (!Godot.FileAccess.FileExists(resourcePath))
        {
            throw new FileNotFoundException($"Scenario data file was not found: {resourcePath}", resourcePath);
        }

        var json = Godot.FileAccess.GetFileAsString(resourcePath);
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions)
                ?? throw new InvalidDataException($"Scenario data file is empty: {resourcePath}");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Scenario data file contains invalid JSON: {resourcePath}", exception);
        }
    }
}
