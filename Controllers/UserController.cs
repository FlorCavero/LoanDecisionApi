using LoanDecisionApi.Data;
using LoanDecisionApi.Models.DTO;
using LoanDecisionApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanDecisionApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController(UserService userService) : ControllerBase
{
    [HttpPost("{id}")]
    public async Task<IActionResult> CreateUser(Guid id, [FromBody] UserCreateRequest request)
    {
        var result = await userService.CreateAsync(id, request);
        
        if (result is null) return NotFound();

        if(result.IsSuccess)
        {
            var response = UserResponse.FromEntity(result.Value!);
            return Created($"{Request.Path}/{response.Id}", response);
        }
        else
        {
            return UnprocessableEntity(result.Errors);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UserUpdateRequest request)
    {
        var result = await userService.UpdateAsync(id, request);

        if (result is null)
        {
            return NotFound();
        }
        if (result.IsSuccess)
        {
            return Ok(UserResponse.FromEntity(result.Value!));
        }
        return UnprocessableEntity(result.Errors);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await userService.GetAsync(id);

        return user is null ? 
            NotFound() :
            Ok(UserResponse.FromEntity(user));           
        
   }
}