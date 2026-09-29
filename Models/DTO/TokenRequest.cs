namespace LoanDecisionApi.Models.DTO;

public record TokenRequest
{
    public string? Name { get; set; }
    public string? Secret { get; set; }
}
