using LoanDecisionApi.Models.Domain;

namespace LoanDecisionApi.Models.DTO;

public record LoanApplicationEvaluationRequest
{
    public string? Ssn { get; set; }
    public decimal? AnnualIncome { get; set; }
    public decimal? RequestedAmount { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public decimal? MonthlyDebtPayments { get; set; }
    public bool? IsIdentityVerified { get; set; }
    public bool? IsFraudRiskFlagged { get; set; }
    public bool? IsCreditFreezeFlagged { get; set; }
    public Delinquency? DelinquencyStatus { get; set; }

}
