using LoanDecisionApi.Models.Domain;

namespace LoanDecisionApi.Models.DTO;

public record RuleCreateRequest
{
    public string? DataPoint { get; set; }
    public RuleCondition? Condition { get; set; }
    public string? Value { get; set; }
}