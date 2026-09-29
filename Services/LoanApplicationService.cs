using LoanDecisionApi.Data;
using LoanDecisionApi.Models.Domain;
using LoanDecisionApi.Models.DTO;

namespace LoanDecisionApi.Services;

public class LoanApplicationService(LoanDecisionDBContext dbContext)
{
    public async Task<Result<LoanApplication>> CreateAsync(LoanApplicationCreateRequest request)
    {
        Result<LoanApplication> result = LoanApplication.Create(request);

        if(result.IsSuccess)
        {
            result.Value!.GetCreditScore();
            result.Value!.GetPreApproval();
            dbContext.LoanApplications.Add(result.Value!);
            await dbContext.SaveChangesAsync();
        }
        return result;
    }

    public async Task<Result<LoanApplication>?> UpdateAsync(Guid id, LoanApplicationUpdateRequest request)
    {
        var loanApplication = await dbContext.LoanApplications.FindAsync(id);
        if (loanApplication is null)
        {
            return null;
        }
        else
        {
            var creditCheckRerunNeeded = request.Ssn != loanApplication.Ssn;
            Result<LoanApplication> result = loanApplication.Update(request);
            if (result.IsSuccess)
            {
                if (creditCheckRerunNeeded) result.Value!.GetCreditScore();
                result.Value!.GetPreApproval();
                await dbContext.SaveChangesAsync();
            }
            return result;
        }
    }

    public async Task<LoanApplication?> GetAsync(Guid id)
    {
        return await dbContext.LoanApplications.FindAsync(id);
     }
}
