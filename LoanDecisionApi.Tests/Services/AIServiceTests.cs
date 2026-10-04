using System.Net;
using System.Text;
using LoanDecisionApi.Models.Domain;
using LoanDecisionApi.Models.DTO;
using LoanDecisionApi.Services;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Xunit;

namespace LoanDecisionApi.Tests.Services;

public class AIServiceTests
{
    // Intercepts every HttpClient request and returns a canned response - this is what
    // lets AIService's real HTTP call be tested without hitting Anthropic's actual API
    // (no network, no cost, no API key needed).
    private sealed class FakeHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }

    private static AIService CreateService(string anthropicResponseJson)
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
    public async Task ParseLoanApplicationRequestAsync_WhenToolUseReturnsData_MapsFieldsCorrectly()
    {
        // Shaped like a real Anthropic Messages API response containing a tool_use block -
        // this is what ExtractToolInput actually parses.
        const string anthropicResponse = """
        {
            "content": [
                {
                    "type": "tool_use",
                    "name": "extract_loan_application",
                    "input": {
                        "ssn": "123456789",
                        "annualIncome": 85000,
                        "dateOfBirth": "1990-03-14",
                        "monthlyDebtPayments": 450,
                        "isIdentityVerified": true,
                        "isFraudRiskFlagged": false,
                        "isCreditFreezeFlagged": false,
                        "delinquencyStatus": "Current"
                    }
                }
            ]
        }
        """;

        var service = CreateService(anthropicResponse);

        var result = await service.ParseLoanApplicationRequestAsync(
            new LoanApplicationEvaluationTextRequest { Text = "some credit profile text" });

        result.Ssn.ShouldBe("123456789");
        result.AnnualIncome.ShouldBe(85000);
        result.DateOfBirth.ShouldBe(new DateOnly(1990, 3, 14));
        result.MonthlyDebtPayments.ShouldBe(450);
        result.IsIdentityVerified.ShouldBe(true);
        result.IsFraudRiskFlagged.ShouldBe(false);
        result.IsCreditFreezeFlagged.ShouldBe(false);
        result.DelinquencyStatus.ShouldBe(Delinquency.Current);
    }
}
