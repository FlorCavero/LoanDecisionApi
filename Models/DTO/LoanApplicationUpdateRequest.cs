namespace LoanDecisionApi.Models.DTO;

public record 
LoanApplicationUpdateRequest
{
    public string? Ssn { get; set; }
    public decimal? AnnualIncome { get; set; }
    public decimal? RequestedAmount { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public decimal? MonthlyDebtPayments { get; set; }
}