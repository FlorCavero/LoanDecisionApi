namespace LoanDecisionApi.Models.Domain;

public static class LoanApplicationValidator
{
    public static bool IsSsnValid(string? ssn) => ssn != null && ssn.Length==9 && ssn.All(s => char.IsDigit(s));
    public static bool IsAnnualIncomeValid(decimal? annualIncome) => annualIncome != null && annualIncome >= 0;
    public static bool IsRequestedAmountValid(decimal? requestedAmount) => requestedAmount != null && requestedAmount > 0;
    public static bool IsDateOfBirthValid(DateOnly? dateOfBirth) => dateOfBirth == null || dateOfBirth <= DateOnly.FromDateTime(DateTime.Now).AddYears(-18);
    public static bool IsMonthlyDebtPaymentsValid(decimal? monthlyDebtPayments) => monthlyDebtPayments == null || monthlyDebtPayments >= 0;
}
