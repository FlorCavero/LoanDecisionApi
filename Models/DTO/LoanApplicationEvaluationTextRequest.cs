namespace LoanDecisionApi.Models.DTO;

public record LoanApplicationEvaluationTextRequest
{
    public string? Text { get; set; }
    public List<Guid>? RuleGroupIds { get; set; }
}
