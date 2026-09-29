using LoanDecisionApi.Models.DTO;
using LoanDecisionApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanDecisionApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RuleGroupController(RuleGroupService ruleGroupService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateRuleGroup([FromBody] RuleGroupCreateRequest request)
    {
        var result = await ruleGroupService.CreateAsync(request);

        if (result.IsSuccess)
        {
            return Created($"{Request.Path}/{result.Value!.Id}", result.Value);
        }

        return UnprocessableEntity(result.Errors);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRuleGroup(Guid id)
    {
        var ruleGroup = await ruleGroupService.GetAsync(id);

        return ruleGroup is null ? NotFound() : Ok(ruleGroup);
    }
}
