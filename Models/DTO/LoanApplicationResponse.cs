using LoanDecisionApi.Models.Domain;

namespace LoanDecisionApi.Models.DTO;

public record LoanApplicationResponse
{
    public required Guid Id { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required string Ssn { get; init; }
    public required decimal AnnualIncome { get; init; }
    public required decimal RequestedAmount { get; init; }
    public int CreditScore { get; init; }
    public required LoanStatus Status { get; init; }

    public static LoanApplicationResponse FromEntity(LoanApplication loanApplication)
    {
        return new LoanApplicationResponse()
        {
            Id = loanApplication.Id,
            CreatedAt = loanApplication.CreatedAt,
            Ssn = "***-**-" + loanApplication.Ssn[5..],
            AnnualIncome = loanApplication.AnnualIncome,
            RequestedAmount =  loanApplication.RequestedAmount,
            CreditScore = loanApplication.CreditScore,
            Status = loanApplication.Status            
        };
    }
}