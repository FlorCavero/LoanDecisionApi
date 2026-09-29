namespace LoanDecisionApi.Models.DTO;

public record Result<T>
{
    public bool IsSuccess {get; }
    public T? Value {get; }
    public List<string>? Errors {get; }

    private Result(bool isSuccess, T? value, List<string>? errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(List<string> errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors), "Failure parameter missing.");
        }
        else if (errors.Count == 0)
        {
            throw new ArgumentException("A failure must include at least one error message.", nameof(errors));
        }
        return new(false, default, errors);
    }

}