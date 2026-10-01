using LoanDecisionApi.Data;
using LoanDecisionApi.Models.Domain;
using LoanDecisionApi.Models.DTO;
using Microsoft.EntityFrameworkCore;

namespace LoanDecisionApi.Services;

public class LoanDecisionService(LoanDecisionDBContext dbContext, AIService aiService)
{
    public async Task<Result<LoanDecisionResponse>?> EvaluateAsync(Guid loanApplicationId, LoanApplicationEvaluationTextRequest textRequest)
    {
        var application = await dbContext.LoanApplications.FindAsync(loanApplicationId);
        if (application is null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(textRequest.Text))
        {
            return Result<LoanDecisionResponse>.Failure(["Missing credit profile information."]);
        }

        var request = await aiService.ParseLoanApplicationRequestAsync(textRequest);

        var requestedIds = textRequest.RuleGroupIds;
        List<RuleGroup> ruleGroups;

        if (requestedIds is null || requestedIds.Count == 0)
        {
            // No specific groups requested - evaluate against everything that exists, in one query.
            ruleGroups = await dbContext.RuleGroups.ToListAsync();
            if (ruleGroups.Count == 0)
            {
                return Result<LoanDecisionResponse>.Failure(["No rule groups are available to evaluate."]);
            }
        }
        else
        {
            // Specific groups requested - fetch them all in one query, then report any
            // requested ids that didn't match a real row, instead of looking each up individually.
            ruleGroups = await dbContext.RuleGroups
                .Where(ruleGroup => requestedIds.Contains(ruleGroup.Id))
                .ToListAsync();

            var missingIds = requestedIds.Except(ruleGroups.Select(ruleGroup => ruleGroup.Id)).ToList();
            if (missingIds.Count > 0)
            {
                return Result<LoanDecisionResponse>.Failure([.. missingIds.Select(id => $"Rule group '{id}' was not found.")]);
            }
        }

        // First, absorb whatever fresh credit-profile/application data arrived with this
        // evaluation request - only then do we know what the rules will actually see.
        var applyResult = application.ApplyEvaluationData(request);
        if (!applyResult.IsSuccess)
        {
            return Result<LoanDecisionResponse>.Failure(applyResult.Errors!);
        }

        var groupResults = ruleGroups.Select(ruleGroup => RuleEvaluator.Evaluate(ruleGroup, application)).ToList();

        // Approved if any product/lender's rule group passed; PendingReview if nothing
        // passed but at least one was indeterminate due to missing data; otherwise every
        // group was conclusively evaluated and failed, so Denied.
        var overallOutcome = groupResults.Any(r => r.Outcome == EvaluationOutcome.Passed)
            ? EvaluationOutcome.Passed
            : groupResults.Any(r => r.Outcome == EvaluationOutcome.Skipped)
                ? EvaluationOutcome.Skipped
                : EvaluationOutcome.Failed;

        application.ApplyEvaluationOutcome(overallOutcome);
        await dbContext.SaveChangesAsync();

        return Result<LoanDecisionResponse>.Success(new LoanDecisionResponse(application.Id, application.Status, groupResults));
    }
}
