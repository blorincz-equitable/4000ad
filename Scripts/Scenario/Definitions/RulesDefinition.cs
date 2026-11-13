namespace Game.Scenarios;

public sealed class RulesDefinition
{
    public RuleDefinition Combat { get; init; } = new();
    public RuleDefinition Production { get; init; } = new();
    public RuleDefinition Movement { get; init; } = new();
    public RuleDefinition Victory { get; init; } = new();
}

public sealed class RuleDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
}
