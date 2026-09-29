using LoanDecisionApi.Models.Domain;

namespace LoanDecisionApi.Models.DTO;

public record RuleGroupCreateRequest
{
    public string? Name { get; set; }
    public RuleGrouping? Grouping { get; set; }
    public List<RuleCreateRequest>? Rules { get; set; }
    public List<RuleGroupCreateRequest>? ChildGroups { get; set; }
}
