using System.Text.Json.Serialization;
using LoanDecisionApi.Models.DTO;

namespace LoanDecisionApi.Models.Domain;

public enum RuleCondition {
    Equals,
    DoesNotEqual,
    GreaterThan,
    GreaterOrEqualTo,
    LessThan,
    LessOrEqualTo,
    In,
    NotIn
}

public enum RuleGrouping {
    Or,
    And
}

public partial class Rule
{
    public Guid Id { get; }
    public string DataPoint { get; private set; }
    public RuleCondition Condition { get; private set; }
    public string Value { get; private set; }

    // Used only by System.Text.Json to round-trip an already-validated Rule to/from
    // the RuleGroup's jsonb column - preserves the original Id, unlike the other
    // constructor below (which always mints a new one for a brand-new Rule).
    [JsonConstructor]
    private Rule(Guid id, string dataPoint, RuleCondition condition, string value)
    {
        Id = id;
        DataPoint = dataPoint;
        Condition = condition;
        Value = value;
    }

    private Rule(RuleCreateRequest ruleCreateRequest)
    {
        Id = Guid.NewGuid();
        DataPoint = ruleCreateRequest.DataPoint!;
        Condition = ruleCreateRequest.Condition!.Value;
        Value = ruleCreateRequest.Value!;
    }

    private static readonly Dictionary<string, Type> DataPointTypes = new()
    {
        ["CreditScore"] = typeof(int),
        ["AnnualIncome"] = typeof(decimal),
        ["RequestedAmount"] = typeof(decimal),
        ["MonthlyDebtPayments"] = typeof(decimal),
        ["DebtToIncomeRatio"] = typeof(decimal),
        ["DateOfBirth"] = typeof(DateOnly),
        ["DelinquencyStatus"] = typeof(Delinquency),
        ["IsIdentityVerified"] = typeof(bool),
        ["IsFraudRiskFlagged"] = typeof(bool),
        ["IsCreditFreezeFlagged"] = typeof(bool),
    };

    public static bool IsValidDataPoint(string? dataPoint) =>
        !string.IsNullOrWhiteSpace(dataPoint) && DataPointTypes.ContainsKey(dataPoint);

    public static bool TryGetDataPointType(string dataPoint, out Type type) =>
        DataPointTypes.TryGetValue(dataPoint, out type!);

    // Shared by both Create's validation and RuleEvaluator's actual comparisons, so there's
    // exactly one place that knows how to turn a Rule's raw string Value into a real,
    // typed value for a given data point.
    public static bool TryParseValue(Type type, string value, out object? parsed)
    {
        parsed = null;
        try
        {
            if (type == typeof(int)) { parsed = int.Parse(value); return true; }
            if (type == typeof(decimal)) { parsed = decimal.Parse(value); return true; }
            if (type == typeof(DateOnly)) { parsed = DateOnly.Parse(value); return true; }
            if (type == typeof(bool)) { parsed = bool.Parse(value); return true; }
            if (type.IsEnum) { parsed = Enum.Parse(type, value, ignoreCase: true); return true; }
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static bool IsValueValidForDataPoint(string? dataPoint, RuleCondition? condition, string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || dataPoint is null || !TryGetDataPointType(dataPoint, out var type))
        {
            return false;
        }

        // In/NotIn compare against a list of values, encoded as a comma-separated string
        // (e.g. "Current,Days30") - every listed value must individually be valid for
        // the data point's type. Every other condition compares against a single value.
        var values = condition is RuleCondition.In or RuleCondition.NotIn
            ? value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            : [value];

        return values.Length > 0 && values.All(v => TryParseValue(type, v, out _));
    }

    public static Result<Rule> Create(RuleCreateRequest request)
    {
        List<string> errors = [];

        if (!IsValidDataPoint(request.DataPoint))
        {
            errors.Add("Invalid data point.");
        }
        else if (!IsValueValidForDataPoint(request.DataPoint, request.Condition, request.Value))
        {
            errors.Add($"Value is not valid for data point '{request.DataPoint}'.");
        }

        if (request.Condition is null) errors.Add("Condition is required.");

        if (errors.Count > 0)
        {
            return Result<Rule>.Failure(errors);
        }

        var rule = new Rule(request);
        return Result<Rule>.Success(rule);
    }

}
