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
    public decimal? MonthlyDebtPayments { get; private set; }
    public decimal? DebtToIncomeRatio => AnnualIncome == 0 || MonthlyDebtPayments is null ? null : (MonthlyDebtPayments * 12) / AnnualIncome;
    public bool? IsIdentityVerified { get; private set; }
    public bool? IsFraudRiskFlagged { get; private set; }
    public bool? IsCreditFreezeFlagged { get; private set; }
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
        if (Status is LoanStatus.PreApproved or LoanStatus.Approved)
        {
            return Result<LoanApplication>.Failure(["Cannot update an application that has been pre-approved or approved. Submit an evaluation request instead."]);
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

    // Separate from Update() by design: this is credit-profile/underwriting data arriving
    // alongside an evaluation request, not an applicant editing their own basic info -
    // it's only valid once the initial screen has already run (PreApproved/PendingReview),
    // the exact opposite gating of Update(). Every field is a partial update: an omitted
    // field keeps its current, already-valid value rather than being reset to unknown -
    // this matters once evaluation requests start being populated from an AI-parsed
    // document that won't always restate every field.
    public Result<LoanApplication> ApplyEvaluationData(LoanApplicationEvaluationRequest request)
    {
        if (Status is not (LoanStatus.PreApproved or LoanStatus.PendingReview))
        {
            return Result<LoanApplication>.Failure(["Evaluation data can only be applied to an application that is pre-approved or needs review."]);
        }

        List<string> errors = [];
        if (request.Ssn != null && !LoanApplicationValidator.IsSsnValid(request.Ssn)) errors.Add("Invalid SSN. It must be 9 digits.");
        if (request.AnnualIncome != null && !LoanApplicationValidator.IsAnnualIncomeValid(request.AnnualIncome)) errors.Add("Invalid annual income. It must be a positive number or 0.");
        if (request.RequestedAmount != null && !LoanApplicationValidator.IsRequestedAmountValid(request.RequestedAmount)) errors.Add("Invalid requested amount. It must be greater than 0.");
        if (request.DateOfBirth != null && !LoanApplicationValidator.IsDateOfBirthValid(request.DateOfBirth)) errors.Add("Invalid date of birth. Must be at 18 years of age or older.");
        if (request.MonthlyDebtPayments != null && !LoanApplicationValidator.IsMonthlyDebtPaymentsValid(request.MonthlyDebtPayments)) errors.Add("MonthlyDebtPayments cannot be a negative number");
        // JsonStringEnumConverter's default allowIntegerValues:true means a raw, out-of-range
        // integer (e.g. 99) still deserializes successfully into an undefined Delinquency -
        // this is the one field here that isn't already fully closed off by its own type.
        if (request.DelinquencyStatus != null && !Enum.IsDefined(request.DelinquencyStatus.Value)) errors.Add("Invalid delinquency status.");

        if (errors.Count > 0)
        {
            return Result<LoanApplication>.Failure(errors);
        }

        if (request.Ssn != null) Ssn = request.Ssn;
        if (request.AnnualIncome != null) AnnualIncome = request.AnnualIncome.Value;
        if (request.RequestedAmount != null) RequestedAmount = request.RequestedAmount.Value;
        if (request.DateOfBirth != null) DateOfBirth = request.DateOfBirth.Value;
        if (request.MonthlyDebtPayments != null) MonthlyDebtPayments = request.MonthlyDebtPayments.Value;
        if (request.IsIdentityVerified != null) IsIdentityVerified = request.IsIdentityVerified;
        if (request.IsFraudRiskFlagged != null) IsFraudRiskFlagged = request.IsFraudRiskFlagged;
        if (request.IsCreditFreezeFlagged != null) IsCreditFreezeFlagged = request.IsCreditFreezeFlagged;
        if (request.DelinquencyStatus != null) DelinquencyStatus = request.DelinquencyStatus.Value;
        ModifiedAt = DateTime.UtcNow;

        return Result<LoanApplication>.Success(this);
    }

    // Applies the final, aggregated outcome across every rule group an evaluation request
    // checked: any group Passed -> Approved; else any group Skipped (indeterminate due to
    // missing data) -> PendingReview; else every group definitively Failed -> Denied.
    public void ApplyEvaluationOutcome(EvaluationOutcome outcome)
    {
        Status = outcome switch
        {
            EvaluationOutcome.Passed => LoanStatus.Approved,
            EvaluationOutcome.Skipped => LoanStatus.PendingReview,
            EvaluationOutcome.Failed => LoanStatus.Denied,
            _ => Status
        };
    }

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
        bool? isIdentityVerified = null,
        bool? isFraudRiskFlagged = null,
        bool? isCreditFreezeFlagged = null,
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
    public void GetCreditScore()
    {
        if (int.TryParse(Ssn[6..], out int seed))
        {
            // seed is 0-999 (Ssn's last 3 digits); scale proportionally into the real
            // FICO range (300-850) instead of an additive shift, which would double the
            // density of the lower half of the range compared to the upper half.
            CreditScore = 300 + (seed * 551 / 1000);
        }
    }

    public void GetPreApproval()
    {
        if (CreditScore == 0)
        {
            Status = LoanStatus.PendingReview;
        }
        else if (CreditScore < 650 || RequestedAmount > AnnualIncome * 0.3m)
        {
            /// Mocked pre-approval check. It should come from the rule engine instead.
            Status = LoanStatus.Denied;            
        }
        else
        {
            Status = LoanStatus.PreApproved;
        }
    }

}