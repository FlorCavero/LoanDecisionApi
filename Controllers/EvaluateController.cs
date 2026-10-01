using LoanDecisionApi.Models.DTO;
using LoanDecisionApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanDecisionApi.Controllers;

[ApiController]
[Route("api/evaluate")]
[Authorize]
public class EvaluateController(LoanDecisionService loanDecisionService) : ControllerBase
{
    [HttpPost("{loanApplicationId}")]
    public async Task<IActionResult> Evaluate(Guid loanApplicationId, [FromBody] LoanApplicationEvaluationTextRequest textRequest)
    {
        var result = await loanDecisionService.EvaluateAsync(loanApplicationId, textRequest);

        if (result is null)
        {
            return NotFound();
        }

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return UnprocessableEntity(result.Errors);
    }
}
