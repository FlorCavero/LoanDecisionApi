using LoanDecisionApi.Models.DTO;
using Microsoft.AspNetCore.Identity;

namespace LoanDecisionApi.Models.Domain;

public class ApiPartner
{
    private static readonly PasswordHasher<ApiPartner> Hasher = new();

    public Guid Id { get; } = Guid.NewGuid();
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public string Name { get; private set; }
    public string HashedSecret { get; private set; }
    public bool IsActive { get; private set; } = true;

    private ApiPartner()
    {
        Name = null!;
        HashedSecret = null!;
    }

    private ApiPartner(string name, string hashedSecret)
    {
        Name = name;
        HashedSecret = hashedSecret;
    }

    public static Result<ApiPartner> Create(string name, string secret)
    {
        List<string> errors = [];
        if (string.IsNullOrWhiteSpace(name)) errors.Add("Partner name is required.");
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 16) errors.Add("Secret must be at least 16 characters.");

        if (errors.Count > 0)
        {
            return Result<ApiPartner>.Failure(errors);
        }

        var partner = new ApiPartner(name, hashedSecret: null!);
        partner.HashedSecret = Hasher.HashPassword(partner, secret);
        return Result<ApiPartner>.Success(partner);
    }

    public bool VerifySecret(string secret)
    {
        if (!IsActive) return false;
        var verification = Hasher.VerifyHashedPassword(this, HashedSecret, secret);
        return verification is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
