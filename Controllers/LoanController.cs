using Microsoft.AspNetCore.Mvc;

namespace LoanDecisionApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoanController : ControllerBase
{
    [HttpPost("decision")]
    public IActionResult EvaluateLoan([FromBody] LoanApplication application)
    {
        var approved = application.CreditScore >= 650 
                    && application.AnnualIncome >= 30000;
        
        var decision = new LoanDecision(
            approved ? "Approved" : "Denied",
            approved ? "Credit score and income meet requirements" 
                     : "Credit score or income below threshold",
            application.CreditScore
        );

        return Ok(decision);
    }
}

public record LoanApplication(string FullName, int CreditScore, decimal AnnualIncome);
public record LoanDecision(string Decision, string Reason, int CreditScore);