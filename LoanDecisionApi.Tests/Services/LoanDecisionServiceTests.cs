using System.Net;
using System.Text;
using LoanDecisionApi.Data;
using LoanDecisionApi.Models.Domain;
using LoanDecisionApi.Models.DTO;
using LoanDecisionApi.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace LoanDecisionApi.Tests.Services;

public class LoanDecisionServiceTests
{
    // A fresh, isolated in-memory database per test - UseInMemoryDatabase's argument
    // is the database's name, and a new Guid guarantees no state leaks between tests.
    private static LoanDecisionDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LoanDecisionDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // EphemeralDataProtectionProvider is a real, non-persistent IDataProtectionProvider
        // built specifically for tests - no need to fake or configure Data Protection at all.
        return new LoanDecisionDBContext(options, new EphemeralDataProtectionProvider());
    }

    // Only safe for tests where EvaluateAsync returns before ever calling AIService
    // (application-not-found, no-text-sent) - anything past that needs CreateFakeAIService.
    private static AIService CreateUnusedAIService()
    {
        var configuration = new ConfigurationBuilder().Build();
        return new AIService(new HttpClient(), configuration);
    }

    // Intercepts every HttpClient request and returns a canned response - lets AIService's
    // real HTTP call be tested without hitting Anthropic's actual API.
    private sealed class FakeHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }

    private static AIService CreateFakeAIService(string anthropicResponseJson)
    {
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(anthropicResponseJson, Encoding.UTF8, "application/json")
        });

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Anthropic:ApiKey"] = "test-key" })
            .Build();

        return new AIService(new HttpClient(handler), configuration);
    }

    [Fact]
    public async Task EvaluateAsync_WhenApplicationDoesNotExist_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var service = new LoanDecisionService(dbContext, CreateUnusedAIService());

        var result = await service.EvaluateAsync(Guid.NewGuid(), new LoanApplicationEvaluationTextRequest { Text = "irrelevant" });

        result.ShouldBeNull();
    }

    [Fact]
    public async Task EvaluateAsync_WhenNoTextSent_ReturnsFalse()
    {
        await using var dbContext = CreateDbContext();
        var service = new LoanDecisionService(dbContext, CreateUnusedAIService());

        var application = LoanApplication.CreateForTesting();
        dbContext.LoanApplications.Add(application);
        await dbContext.SaveChangesAsync();

        var result = await service.EvaluateAsync(application.Id, new LoanApplicationEvaluationTextRequest { });

        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_WhenNoRuleGroup_ReturnsFalse()
    {
        await using var dbContext = CreateDbContext();
        var service = new LoanDecisionService(dbContext, CreateFakeAIService("""{ "content": [] }"""));

        // Must be PreApproved/PendingReview - ApplyEvaluationData's own status gate
        // would otherwise fail this request before the "no rule group" check is ever reached.
        var application = LoanApplication.CreateForTesting(status: LoanStatus.PreApproved);
        dbContext.LoanApplications.Add(application);
        await dbContext.SaveChangesAsync();

        var result = await service.EvaluateAsync(application.Id, new LoanApplicationEvaluationTextRequest { Text = "irrelevant" });

        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_WhenInvalidRuleGroupId_ReturnsFalse()
    {
        await using var dbContext = CreateDbContext();
        var service = new LoanDecisionService(dbContext, CreateFakeAIService("""{ "content": [] }"""));

        var application = LoanApplication.CreateForTesting(status: LoanStatus.PreApproved);
        dbContext.LoanApplications.Add(application);
        await dbContext.SaveChangesAsync();

        var result = await service.EvaluateAsync(
            application.Id,
            new LoanApplicationEvaluationTextRequest { Text = "irrelevant", RuleGroupIds = [new Guid()] });

        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_RuleGroup_ReturnsTrue()
    {
        await using var dbContext = CreateDbContext();

        const string anthropicResponse = """
        {
            "content": [
                {
                    "type": "tool_use",
                    "name": "extract_loan_application",
                    "input": { "annualIncome": 70000 }
                }
            ]
        }
        """;
        var service = new LoanDecisionService(dbContext, CreateFakeAIService(anthropicResponse));

        var application = LoanApplication.CreateForTesting(status: LoanStatus.PreApproved);
        dbContext.LoanApplications.Add(application);

        var rule = Rule.CreateForTesting("AnnualIncome", RuleCondition.GreaterOrEqualTo, "50000");
        var ruleGroup = RuleGroup.CreateForTesting("Test", RuleGrouping.And, [rule]);
        dbContext.RuleGroups.Add(ruleGroup);

        await dbContext.SaveChangesAsync();

        var result = await service.EvaluateAsync(
            application.Id,
            new LoanApplicationEvaluationTextRequest { Text = "Annual income is 70000", RuleGroupIds = [ruleGroup.Id] });

        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
    }
}
