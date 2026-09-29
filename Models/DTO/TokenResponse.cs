namespace LoanDecisionApi.Models.DTO;

public record TokenResponse
{
    public required string AccessToken { get; init; }
    public required DateTime ExpiresAt { get; init; }
}
