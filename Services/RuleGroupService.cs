using LoanDecisionApi.Data;
using LoanDecisionApi.Models.Domain;
using LoanDecisionApi.Models.DTO;

namespace LoanDecisionApi.Services;

public class RuleGroupService(LoanDecisionDBContext dbContext)
{
    public async Task<Result<RuleGroup>> CreateAsync(RuleGroupCreateRequest request)
    {
        var result = RuleGroup.Create(request);

        if (result.IsSuccess)
        {
            dbContext.RuleGroups.Add(result.Value!);
            await dbContext.SaveChangesAsync();
        }

        return result;
    }

    public async Task<RuleGroup?> GetAsync(Guid id)
    {
        return await dbContext.RuleGroups.FindAsync(id);
    }
}
