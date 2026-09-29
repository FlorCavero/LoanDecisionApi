using LoanDecisionApi.Data;
using LoanDecisionApi.Models.DTO;
using LoanDecisionApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoanDecisionApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(LoanDecisionDBContext dbContext, JwtTokenService tokenService) : ControllerBase
{
    [HttpPost("token")]
    public async Task<IActionResult> IssueToken([FromBody] TokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Secret))
        {
            return Unauthorized();
        }

        var partner = await dbContext.ApiPartners.SingleOrDefaultAsync(p => p.Name == request.Name);

        // Deliberately return the same 401 whether the partner doesn't exist or the secret is wrong,
        // so a caller can't tell which case it hit and enumerate valid partner names.
        if (partner is null || !partner.VerifySecret(request.Secret))
        {
            return Unauthorized();
        }

        var token = tokenService.GenerateToken(partner);
        return Ok(token);
    }
}
