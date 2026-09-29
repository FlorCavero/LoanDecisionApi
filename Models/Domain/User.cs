using System.Text.RegularExpressions;
using LoanDecisionApi.Models.DTO;
using Microsoft.AspNetCore.Identity;

namespace LoanDecisionApi.Models.Domain;

public partial class User
{
    private static readonly PasswordHasher<User> Hasher = new();
    private static readonly Regex EmailRegex = MyRegex();

    public Guid Id { get; } = Guid.NewGuid();
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public string Email { get; private set; }
    public string HashedPassword { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public bool IsActive { get; private set; } = true;

    private User()
    {
        Email = null!;
        HashedPassword = null!;
        FirstName = null!;
        LastName = null!;
    }

    private User(UserCreateRequest userCreateRequest)
    {
        Email = userCreateRequest.Email!;
        FirstName = userCreateRequest.FirstName!;
        LastName = userCreateRequest.LastName!;
        HashedPassword = null!;
    }

    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        return EmailRegex.IsMatch(email.Trim());
    }

    public static bool IsValidPassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            return false;
        }

        return password.Any(char.IsUpper)
            && password.Any(char.IsLower)
            && password.Any(char.IsDigit)
            && password.Any(ch => !char.IsLetterOrDigit(ch));
    }

    public static Result<User> Create(LoanApplication loanApplication, UserCreateRequest request)
    {
        List<string> errors = [];
        var validStatuses = new[] { LoanStatus.PreApproved, LoanStatus.PendingReview };

        if (loanApplication.UserId is not null) return Result<User>.Failure(["An account has already been created for this application."]);
        if (loanApplication.Ssn != request.Ssn) return Result<User>.Failure(["SSN is not consistent with the provided application id"]);
        if (!validStatuses.Contains(loanApplication.Status)) return Result<User>.Failure([$"Cannot create an account in {loanApplication.Status} status"]);

        if (!IsValidEmail(request.Email)) errors.Add("Invalid email format.");
        if (string.IsNullOrWhiteSpace(request.FirstName)) errors.Add("First name is required.");
        if (string.IsNullOrWhiteSpace(request.LastName)) errors.Add("Last name is required.");
        if (!IsValidPassword(request.Password)) errors.Add("Password must be at least 8 characters and contain at least one uppercase letter, one lowercase letter, one number, and one symbol.");

        if (errors.Count > 0)
        {
            return Result<User>.Failure(errors);
        }

        var user = new User(request);
        user.HashedPassword = Hasher.HashPassword(user, request.Password!);
        return Result<User>.Success(user);
    }

    public Result<User> Update(UserUpdateRequest request)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(request.OldPassword) || !VerifyPassword(request.OldPassword))
        {
            return Result<User>.Failure(["Unable to verify the current password"]);
        }

        if (!IsValidEmail(request.Email)) errors.Add("Invalid email format.");
        if (string.IsNullOrWhiteSpace(request.FirstName)) errors.Add("First name is required.");
        if (string.IsNullOrWhiteSpace(request.LastName)) errors.Add("Last name is required.");
        if (!string.IsNullOrWhiteSpace(request.NewPassword) && !IsValidPassword(request.NewPassword)) errors.Add("Password must be at least 8 characters and contain at least one uppercase letter, one lowercase letter, one number, and one symbol.");

        if (errors.Count > 0)
        {
            return Result<User>.Failure(errors);
        }

        Email = request.Email!;
        FirstName = request.FirstName!;
        LastName = request.LastName!;
        if(!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            HashedPassword = Hasher.HashPassword(this, request.NewPassword!);        
        }
        return Result<User>.Success(this);
    }

    public bool VerifyPassword(string password)
    {
        if (!IsActive) return false;
        var verification = Hasher.VerifyHashedPassword(this, HashedPassword, password);
        return verification is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex MyRegex();

}
