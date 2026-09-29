namespace LoanDecisionApi.Models.DTO;

public record LoanApplicationCreateRequest
{
    public string? Ssn { get; set; }
    public decimal? AnnualIncome { get; set; }
    public decimal? RequestedAmount { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public bool? IsKeyedIn { get; set; }
}