using LoanDecisionApi.Models.Domain;

namespace LoanDecisionApi.Models.DTO;

public record UserResponse
{
    public required Guid Id { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }

    public static UserResponse FromEntity(User user)
    {
        return new UserResponse()
        {
            Id = user.Id,
            CreatedAt = user.CreatedAt,
            FirstName = user.FirstName,
            LastName =  user.LastName,
            Email = user.Email,
        };
    }
}