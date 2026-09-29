using LoanDecisionApi.Data;
using LoanDecisionApi.Models.Domain;
using LoanDecisionApi.Models.DTO;

namespace LoanDecisionApi.Services;

public class UserService(LoanDecisionDBContext dbContext)
{
    public async Task<Result<User>?> CreateAsync(Guid id, UserCreateRequest request)
    {
        var loanApplication = await dbContext.LoanApplications.FindAsync(id);

        if (loanApplication is null) return null;

        Result<User> result = User.Create(loanApplication, request);

        if(result.IsSuccess)
        {
            loanApplication.AssignUser(result.Value!.Id);
            dbContext.Users.Add(result.Value!);
            await dbContext.SaveChangesAsync();
        }
        return result;
    }

    public async Task<Result<User>?> UpdateAsync(Guid id, UserUpdateRequest request)
    {
        var user = await dbContext.Users.FindAsync(id);
        if (user is null)
        {
            return null;
        }
        else
        {
            Result<User> result = user.Update(request);
            if (result.IsSuccess)
            {
                await dbContext.SaveChangesAsync();
            }
            return result;
        }
    }

    public async Task<User?> GetAsync(Guid id)
    {
        return await dbContext.Users.FindAsync(id);
     }
}
