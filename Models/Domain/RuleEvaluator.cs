namespace LoanDecisionApi.Models.Domain;

// Skipped means "could not be determined" - either the data point needed for this rule
// hasn't been provided yet (e.g. fraud check hasn't run), or the rule itself is malformed
// (unknown data point, unparseable value). Neither case means the applicant failed
// anything, so it's kept distinct from Failed rather than defaulting to one or the other.
public enum EvaluationOutcome
{
    Passed,
    Failed,
    Skipped
}

public record RuleEvaluationResult(Guid RuleId, string DataPoint, EvaluationOutcome Outcome, string Details);

public record RuleGroupEvaluationResult(
    Guid RuleGroupId,
    string Name,
    string GroupingType,
    EvaluationOutcome Outcome,
    List<RuleEvaluationResult> RuleResults,
    List<RuleGroupEvaluationResult> ChildResults);

public static class RuleEvaluator
{
    public static RuleGroupEvaluationResult Evaluate(RuleGroup group, LoanApplication application)
    {
        var ruleResults = group.Rules.Select(rule => EvaluateRule(rule, application)).ToList();
        var childResults = group.ChildGroups.Select(child => Evaluate(child, application)).ToList();

        var outcomes = ruleResults.Select(r => r.Outcome)
            .Concat(childResults.Select(c => c.Outcome))
            .ToList();

        var (outcome, groupingType) = group.Grouping switch
        {
            RuleGrouping.And => (CombineAnd(outcomes), "All"),
            RuleGrouping.Or => (CombineOr(outcomes), "Any"),
            _ => throw new InvalidOperationException($"Unsupported condition '{group.Grouping}' for evaluation.")
        };

        return new RuleGroupEvaluationResult(group.Id, group.Name, groupingType, outcome, ruleResults, childResults);
    }

    // Three-valued (Kleene) logic - the same semantics SQL uses for NULL propagation through
    // AND/OR - so "unknown" never gets silently treated as a pass or a fail.
    private static EvaluationOutcome CombineAnd(List<EvaluationOutcome> outcomes)
    {
        if (outcomes.Any(o => o == EvaluationOutcome.Failed)) return EvaluationOutcome.Failed;
        if (outcomes.Any(o => o == EvaluationOutcome.Skipped)) return EvaluationOutcome.Skipped;
        return EvaluationOutcome.Passed;
    }

    private static EvaluationOutcome CombineOr(List<EvaluationOutcome> outcomes)
    {
        if (outcomes.Any(o => o == EvaluationOutcome.Passed)) return EvaluationOutcome.Passed;
        if (outcomes.Any(o => o == EvaluationOutcome.Skipped)) return EvaluationOutcome.Skipped;
        return EvaluationOutcome.Failed;
    }

    private static RuleEvaluationResult EvaluateRule(Rule rule, LoanApplication application)
    {
        if (!Rule.TryGetDataPointType(rule.DataPoint, out var type))
        {
            return new RuleEvaluationResult(rule.Id, rule.DataPoint, EvaluationOutcome.Skipped, $"Unknown data point '{rule.DataPoint}' - rule skipped.");
        }

        var property = typeof(LoanApplication).GetProperty(rule.DataPoint);
        var actualValue = property?.GetValue(application);
        if (property is null || actualValue is null || IsUnknown(actualValue))
        {
            return new RuleEvaluationResult(rule.Id, rule.DataPoint, EvaluationOutcome.Skipped, $"'{rule.DataPoint}' is not yet known - rule skipped.");
        }

        if (rule.Condition is RuleCondition.In or RuleCondition.NotIn)
        {
            var candidates = rule.Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var isIn = candidates.Any(c => Rule.TryParseValue(type, c, out var parsed) && Equals(actualValue, parsed));
            var inPassed = rule.Condition == RuleCondition.In ? isIn : !isIn;

            // Describe what was actually observed (isIn) separately from the condition being
            // checked and the outcome, rather than layering a conditional "not " onto a verb
            // that may already contain "not" - that combination produced "is not not in".
            return new RuleEvaluationResult(rule.Id, rule.DataPoint, inPassed ? EvaluationOutcome.Passed : EvaluationOutcome.Failed,
                $"{rule.DataPoint} ({actualValue}) {(isIn ? "is in" : "is not in")} [{rule.Value}] ({rule.Condition}): {(inPassed ? "passed" : "failed")}.");
        }

        if (!Rule.TryParseValue(type, rule.Value, out var comparisonValue) || comparisonValue is null)
        {
            return new RuleEvaluationResult(rule.Id, rule.DataPoint, EvaluationOutcome.Skipped,
                $"Rule value '{rule.Value}' could not be parsed for '{rule.DataPoint}' - rule skipped.");
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

        return new RuleEvaluationResult(rule.Id, rule.DataPoint, passed ? EvaluationOutcome.Passed : EvaluationOutcome.Failed,
            $"{rule.DataPoint} ({actualValue}) {DescribeCondition(rule.Condition)} {rule.Value}: {(passed ? "passed" : "failed")}.");
    }

    // Sentinel values that mean "not yet known" for data points that can't be genuinely
    // null (value types) - CreditScore is 0 until GetCreditScore() runs, DelinquencyStatus
    // defaults to Unspecified. bool? fields are handled by the plain null check above.
    private static bool IsUnknown(object actualValue) => actualValue switch
    {
        int score => score == 0,
        Delinquency delinquency => delinquency == Delinquency.Unspecified,
        _ => false
    };

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
