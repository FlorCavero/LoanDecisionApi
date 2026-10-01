using LoanDecisionApi.Models.Domain;

namespace LoanDecisionApi.Models.DTO;

public record LoanDecisionResponse(Guid LoanApplicationId, LoanStatus Status, List<RuleGroupEvaluationResult> GroupResults);
