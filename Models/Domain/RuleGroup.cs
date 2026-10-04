using System.Text.Json.Serialization;
using LoanDecisionApi.Models.DTO;

namespace LoanDecisionApi.Models.Domain;

public class RuleGroup
{
    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string Name { get; private set; }
    public RuleGrouping Grouping { get; private set; }
    public List<Rule> Rules { get; private set; }
    public List<RuleGroup> ChildGroups { get; private set; }

    private RuleGroup()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        Name = null!;
        Rules = [];
        ChildGroups = [];
    }

    // Used only by System.Text.Json to round-trip an already-validated RuleGroup to/from
    // its own jsonb column (as a nested ChildGroup) or the top-level EF-mapped row -
    // preserves the original Id/CreatedAt, unlike the constructor below.
    [JsonConstructor]
    private RuleGroup(Guid id, DateTime createdAt, string name, RuleGrouping grouping, List<Rule> rules, List<RuleGroup> childGroups)
    {
        Id = id;
        CreatedAt = createdAt;
        Name = name;
        Grouping = grouping;
        Rules = rules;
        ChildGroups = childGroups;
    }

    private RuleGroup(string name, RuleGrouping grouping, List<Rule> rules, List<RuleGroup> childGroups) : this()
    {
        Name = name;
        Grouping = grouping;
        Rules = rules;
        ChildGroups = childGroups;
    }

    public static Result<RuleGroup> Create(RuleGroupCreateRequest request)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(request.Name)) errors.Add("Name is required.");
        if (request.Grouping is null) errors.Add("Grouping is required.");

        var hasRules = request.Rules is { Count: > 0 };
        var hasChildGroups = request.ChildGroups is { Count: > 0 };
        if (!hasRules && !hasChildGroups) errors.Add("At least one rule or child group is required.");

        List<Rule> rules = [];
        if (request.Rules is not null)
        {
            for (int i = 0; i < request.Rules.Count; i++)
            {
                var ruleResult = Rule.Create(request.Rules[i]);
                if (ruleResult.IsSuccess)
                {
                    rules.Add(ruleResult.Value!);
                }
                else
                {
                    errors.AddRange(ruleResult.Errors!.Select(e => $"Rule {i + 1}: {e}"));
                }
            }
        }

        List<RuleGroup> childGroups = [];
        if (request.ChildGroups is not null)
        {
            for (int i = 0; i < request.ChildGroups.Count; i++)
            {
                var childResult = Create(request.ChildGroups[i]);
                if (childResult.IsSuccess)
                {
                    childGroups.Add(childResult.Value!);
                }
                else
                {
                    errors.AddRange(childResult.Errors!.Select(e => $"Child group {i + 1}: {e}"));
                }
            }
        }

        if (errors.Count > 0)
        {
            return Result<RuleGroup>.Failure(errors);
        }

        var group = new RuleGroup(request.Name!, request.Grouping!.Value, rules, childGroups);
        return Result<RuleGroup>.Success(group);
    }

    // Test-only: lets unit tests pin down RuleGroup fields. Visible only to LoanDecisionApi.Tests via InternalsVisibleTo.
    internal static RuleGroup CreateForTesting(string name, RuleGrouping grouping, List<Rule> rules)
    {
        return new RuleGroup(name, grouping, rules, []);
    }

}
