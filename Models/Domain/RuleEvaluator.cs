namespace LoanDecisionApi.Models.Domain;

public record RuleEvaluationResult(Guid RuleId, string DataPoint, bool Passed, string Details);

public record RuleGroupEvaluationResult(
    Guid RuleGroupId,
    string Name,
    bool Passed,
    List<RuleEvaluationResult> RuleResults,
    List<RuleGroupEvaluationResult> ChildResults);

public static class RuleEvaluator
{
    public static RuleGroupEvaluationResult Evaluate(RuleGroup group, LoanApplication application)
    {
        var ruleResults = group.Rules.Select(rule => EvaluateRule(rule, application)).ToList();
        var childResults = group.ChildGroups.Select(child => Evaluate(child, application)).ToList();

        var outcomes = ruleResults.Select(r => r.Passed)
            .Concat(childResults.Select(c => c.Passed))
            .ToList();

        // Create() guarantees a group always has at least one rule or child group, so this
        // is unreachable in practice - kept as a safe, defined default rather than a crash.
        var passed = outcomes.Count == 0
            ? true
            : group.Grouping == RuleGrouping.And ? outcomes.All(o => o) : outcomes.Any(o => o);

        return new RuleGroupEvaluationResult(group.Id, group.Name, passed, ruleResults, childResults);
    }

    private static RuleEvaluationResult EvaluateRule(Rule rule, LoanApplication application)
    {
        if (!Rule.TryGetDataPointType(rule.DataPoint, out var type))
        {
            return new RuleEvaluationResult(rule.Id, rule.DataPoint, false, $"Unknown data point '{rule.DataPoint}'.");
        }

        var property = typeof(LoanApplication).GetProperty(rule.DataPoint);
        var actualValue = property?.GetValue(application);
        if (property is null || actualValue is null)
        {
            return new RuleEvaluationResult(rule.Id, rule.DataPoint, false, $"'{rule.DataPoint}' has no value to evaluate.");
        }

        if (rule.Condition is RuleCondition.In or RuleCondition.NotIn)
        {
            var candidates = rule.Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var isIn = candidates.Any(c => Rule.TryParseValue(type, c, out var parsed) && Equals(actualValue, parsed));
            var inPassed = rule.Condition == RuleCondition.In ? isIn : !isIn;
            var verb = rule.Condition == RuleCondition.In ? "in" : "not in";

            return new RuleEvaluationResult(rule.Id, rule.DataPoint, inPassed,
                $"{rule.DataPoint} ({actualValue}) is {(inPassed ? "" : "not ")}{verb} [{rule.Value}].");
        }

        if (!Rule.TryParseValue(type, rule.Value, out var comparisonValue) || comparisonValue is null)
        {
            return new RuleEvaluationResult(rule.Id, rule.DataPoint, false,
                $"Rule value '{rule.Value}' could not be parsed for '{rule.DataPoint}'.");
        }

        var comparison = ((IComparable)actualValue).CompareTo(comparisonValue);
        var passed = rule.Condition switch
        {
            RuleCondition.Equals => comparison == 0,
            RuleCondition.DoesNotEqual => comparison != 0,
            RuleCondition.GreaterThan => comparison > 0,
            RuleCondition.GreaterOrEqualTo => comparison >= 0,
            RuleCondition.LessThan => comparison < 0,
            RuleCondition.LessOrEqualTo => comparison <= 0,
            _ => throw new InvalidOperationException($"Unsupported condition '{rule.Condition}' for evaluation.")
        };

        return new RuleEvaluationResult(rule.Id, rule.DataPoint, passed,
            $"{rule.DataPoint} ({actualValue}) {DescribeCondition(rule.Condition)} {rule.Value}: {(passed ? "passed" : "failed")}.");
    }

    private static string DescribeCondition(RuleCondition condition) => condition switch
    {
        RuleCondition.Equals => "==",
        RuleCondition.DoesNotEqual => "!=",
        RuleCondition.GreaterThan => ">",
        RuleCondition.GreaterOrEqualTo => ">=",
        RuleCondition.LessThan => "<",
        RuleCondition.LessOrEqualTo => "<=",
        _ => condition.ToString()
    };
}
