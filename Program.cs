var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.MapGet("/", () => "Loan Decision API is running");

app.MapPost("/loan-decision", (LoanApplication application) =>
{
    var decision = EvaluateLoan(application);
    return Results.Ok(decision);
})
.WithName("EvaluateLoanApplication");

app.Run();

static LoanDecision EvaluateLoan(LoanApplication application)
{
    var approved = application.CreditScore >= 650 && application.AnnualIncome >= 30000;
    var reason = approved
        ? "Credit score and income meet requirements"
        : "Credit score or income below threshold";

    return new LoanDecision(approved ? "Approved" : "Denied", reason, application.CreditScore);
}

record LoanApplication(string FullName, int CreditScore, decimal AnnualIncome);
record LoanDecision(string Decision, string Reason, int CreditScore);