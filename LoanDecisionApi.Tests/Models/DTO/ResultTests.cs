using LoanDecisionApi.Models.DTO;
using Shouldly;


namespace LoanDecisionApi.Tests.Models.DTO;

public class ResultTests
{
    [Fact]
    public void Success_SetsIsSuccessTrue_AndCarriesTheValue()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
        result.Errors.ShouldBeNull();
    }

    [Fact]
    public void Failure_SetsIsSuccessFalse_AndCarriesTheErrors()
    {
        var result = Result<int>.Failure(["something went wrong"]);

        result.IsSuccess.ShouldBeFalse();
        result.Value.ShouldBe(default);
        result.Errors.ShouldHaveSingleItem().ShouldBe("something went wrong");
    }

    [Fact]
    public void Failure_WithNullErrors_ThrowsArgumentNullException()
    {
        var exception = Should.Throw<ArgumentNullException>(() => Result<int>.Failure(null!));

        exception.ParamName.ShouldBe("errors");
    }

    [Fact]
    public void Failure_WithEmptyErrors_ThrowsArgumentException()
    {
        var exception = Should.Throw<ArgumentException>(() => Result<int>.Failure([]));

        exception.ParamName.ShouldBe("errors");
    }
}
