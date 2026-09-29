using LoanDecisionApi.Models.Domain;
using LoanDecisionApi.Models.DTO;
using Shouldly;


namespace LoanDecisionApi.Tests.Models.DTO;

public class LoanApplicationValidatorTests
{
    [Fact]
    public void Success_SetsIsSsnValidTrue()
    {
        var result = LoanApplicationValidator.IsSsnValid("012345678");
        result.ShouldBeTrue();
    }

    [Fact]
    public void Failure_SetsIsSsnValidFalse_Null()
    {
        var result = LoanApplicationValidator.IsSsnValid(null);
        result.ShouldBeFalse();
    }

    [Fact]
    public void Failure_SetsIsSsnValidFalse_WrongLength()
    {
        var result = LoanApplicationValidator.IsSsnValid("0123456789");
        result.ShouldBeFalse();
    }

    [Fact]
    public void Failure_SetsIsSsnValidFalse_NotAllDigits()
    {
        var result = LoanApplicationValidator.IsSsnValid("123-45-6789");
        result.ShouldBeFalse();
    }

    [Fact]
    public void Success_SetsIsSuccessTrue()
    {
        var result = LoanApplicationValidator.IsAnnualIncomeValid(10000);
        result.ShouldBeTrue();
    }

    [Fact]
    public void Failure_SetsIsSuccessFalse_Negative()
    {
        var result = LoanApplicationValidator.IsAnnualIncomeValid(-1);
        result.ShouldBeFalse();
    }

    [Fact]
    public void Success_SetsIsRequestedAmountValid()
    {
        var result = LoanApplicationValidator.IsRequestedAmountValid(10000);
        result.ShouldBeTrue();
    }

    [Fact]
    public void Failure_SetsIsRequestedAmountValid_Zero()
    {
        var result = LoanApplicationValidator.IsRequestedAmountValid(0);
        result.ShouldBeFalse();
    }
}
