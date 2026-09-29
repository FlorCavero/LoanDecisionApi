using LoanDecisionApi.Data;
using LoanDecisionApi.Models.DTO;
using LoanDecisionApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanDecisionApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LoanApplicationController(LoanApplicationService loanApplicationService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateLoanApplication([FromBody] LoanApplicationCreateRequest request)
    {
        var result = await loanApplicationService.CreateAsync(request);

        if(result.IsSuccess)
        {
            var loanApplication = result.Value;
            var response = LoanApplicationResponse.FromEntity(loanApplication!);
            return Created($"{Request.Path}/{response.Id}", response);
        }
        else
        {
            return UnprocessableEntity(result.Errors);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateLoanApplication(Guid id, [FromBody] LoanApplicationUpdateRequest request)
    {
        var result = await loanApplicationService.UpdateAsync(id, request);

        if (result is null)
        {
            return NotFound();
        }
        if (result.IsSuccess)
        {
            return Ok(LoanApplicationResponse.FromEntity(result.Value!));
        }
        return UnprocessableEntity(result.Errors);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLoanApplication(Guid id)
    {
        var loanApplication = await loanApplicationService.GetAsync(id);

        return loanApplication is null ? 
            NotFound() :
            Ok(LoanApplicationResponse.FromEntity(loanApplication));           
        
   }
}