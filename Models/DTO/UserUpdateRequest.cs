namespace LoanDecisionApi.Models.DTO;

public record UserUpdateRequest
{
    public string? Email { get; set; }
    public string? OldPassword { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? NewPassword { get; set; }
}