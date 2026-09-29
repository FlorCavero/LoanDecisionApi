using LoanDecisionApi.Models.DTO;

namespace LoanDecisionApi.Models.Domain;
public enum LoanStatus {
    Entered,
    PendingReview,
    PreApproved,
    Approved,
    Denied
}

public enum Delinquency {
    Unspecified,
    Current,
    Days30,
    Days60,
    Days90Plus,
}

public class LoanApplication
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid? UserId { get; private set; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; private set; }
    public string Ssn { get; private set; }
    public decimal AnnualIncome { get; private set; }
    public decimal RequestedAmount { get; private set; }
    public int CreditScore { get; private set;}
    public DateOnly DateOfBirth { get; private set; }
    public decimal MonthlyDebtPayments { get; private set; }
    public decimal DebtToIncomeRatio => AnnualIncome == 0 ? decimal.MaxValue : (MonthlyDebtPayments * 12) / AnnualIncome;
    public bool IsIdentityVerified { get; private set; }
    public bool IsFraudRiskFlagged { get; private set; }
    public bool IsCreditFreezeFlagged { get; private set; }
    public Delinquency DelinquencyStatus { get; private set; }
    public LoanStatus Status { get; private set;}
    public bool IsKeyedIn { get; private set; }

    private LoanApplication()
    {
        Ssn = null!;
    }

    private LoanApplication(LoanApplicationCreateRequest createRequest)
    {

        Ssn = createRequest.Ssn!;
        AnnualIncome = (decimal)createRequest.AnnualIncome!;
        RequestedAmount = (decimal)createRequest.RequestedAmount!;
        IsKeyedIn = createRequest.IsKeyedIn??false;
        ModifiedAt = CreatedAt;
    }

    public static Result<LoanApplication> Create(LoanApplicationCreateRequest createRequest)
    {
        List<string> errors = [];
        if (!LoanApplicationValidator.IsSsnValid(createRequest.Ssn)) errors.Add("Invalid SSN. It must be 9 digits.");
        if (!LoanApplicationValidator.IsAnnualIncomeValid(createRequest.AnnualIncome)) errors.Add("Invalid annual income. It must be a positive number or 0.");
        if (!LoanApplicationValidator.IsRequestedAmountValid(createRequest.RequestedAmount)) errors.Add("Invalid requested amount. It must be greater than 0.");
        if (!LoanApplicationValidator.IsDateOfBirthValid(createRequest.DateOfBirth)) errors.Add("Invalid date of birth. Must be at 18 years of age or older.");

        return errors.Count > 0 ?
            Result<LoanApplication>.Failure(errors):
            Result<LoanApplication>.Success(new LoanApplication(createRequest));
    }

    public Result<LoanApplication> Update(LoanApplicationUpdateRequest updateRequest)
    {
        if (Status is LoanStatus.Approved or LoanStatus.Denied)
        {
            return Result<LoanApplication>.Failure(["Cannot update a finalized loan application"]);
        }

        List<string> errors = [];
        if (!LoanApplicationValidator.IsSsnValid(updateRequest.Ssn)) errors.Add("Invalid SSN. It must be 9 digits.");
        if (!LoanApplicationValidator.IsAnnualIncomeValid(updateRequest.AnnualIncome)) errors.Add("Invalid annual income. It must be a positive number or 0.");
        if (!LoanApplicationValidator.IsRequestedAmountValid(updateRequest.RequestedAmount)) errors.Add("Invalid requested amount. It must be greater than 0.");
        if (!LoanApplicationValidator.IsDateOfBirthValid(updateRequest.DateOfBirth)) errors.Add("Invalid date of birth. Must be at 18 years of age or older.");
        if (!LoanApplicationValidator.IsMonthlyDebtPaymentsValid(updateRequest.MonthlyDebtPayments)) errors.Add("MonthlyDebtPayments cannot be a negative number");

        if (errors.Count == 0)
        {
            Ssn = updateRequest.Ssn!;
            AnnualIncome  = (decimal)updateRequest.AnnualIncome!;
            RequestedAmount = (decimal)updateRequest.RequestedAmount!;
            ModifiedAt = DateTime.UtcNow;
            if ( updateRequest.DateOfBirth != null ) DateOfBirth = (DateOnly)updateRequest.DateOfBirth; 
            if ( updateRequest.MonthlyDebtPayments != null ) MonthlyDebtPayments = (decimal)updateRequest.MonthlyDebtPayments;
        }
    
        return errors.Count > 0 ?
            Result<LoanApplication>.Failure(errors):
            Result<LoanApplication>.Success(this);
    }
    public void AssignUser(Guid guid) => UserId = guid;

    // Test-only: lets unit tests pin down fields (CreditScore, DelinquencyStatus, etc.)
    // that production code deliberately has no public way to set directly - CreditScore
    // only ever comes from the random GetCreditScore() stub, and DelinquencyStatus has
    // no setter path yet at all. Visible only to LoanDecisionApi.Tests via InternalsVisibleTo.
    internal static LoanApplication CreateForTesting(
        decimal annualIncome = 0,
        decimal requestedAmount = 0,
        int creditScore = 0,
        decimal monthlyDebtPayments = 0,
        Delinquency delinquencyStatus = Delinquency.Unspecified,
        bool isIdentityVerified = false,
        bool isFraudRiskFlagged = false,
        bool isCreditFreezeFlagged = false,
        LoanStatus status = LoanStatus.Entered)
    {
        return new LoanApplication
        {
            Ssn = "123456789",
            AnnualIncome = annualIncome,
            RequestedAmount = requestedAmount,
            CreditScore = creditScore,
            MonthlyDebtPayments = monthlyDebtPayments,
            DelinquencyStatus = delinquencyStatus,
            IsIdentityVerified = isIdentityVerified,
            IsFraudRiskFlagged = isFraudRiskFlagged,
            IsCreditFreezeFlagged = isCreditFreezeFlagged,
            Status = status,
        };
    }

    // This would be pulling from a credit check api, then it will need failure handling to flag PendingReview, and log it.
    public void GetCreditScore() => CreditScore = Random.Shared.Next(300, 999);

    public void GetPreApproval()
    {
        if (CreditScore == 0)
        {
            Status = LoanStatus.PendingReview;
        }
        else if (CreditScore < 650 || AnnualIncome <= RequestedAmount)
        {
            Status = LoanStatus.Denied;            
        }
        else
        {
            Status = LoanStatus.PreApproved;
        }
    }

}