using LoanDecisionApi.Models.Domain;
using LoanDecisionApi.Models.DTO;
using Shouldly;
using Xunit;

namespace LoanDecisionApi.Tests.Models.Domain;

public class RuleEvaluatorTests
{
    // Approve if (CreditScore >= 700 AND DTI <= 0.4)
    //          OR (CreditScore >= 600 AND (DTI <= 0.3 OR DelinquencyStatus In [Current, Days30]))
    private static RuleGroup BuildApprovalPolicy()
    {
        var request = new RuleGroupCreateRequest
        {
            Name = "Approval Policy",
            Grouping = RuleGrouping.Or,
            ChildGroups =
            [
                new RuleGroupCreateRequest
                {
                    Name = "High Credit Path",
                    Grouping = RuleGrouping.And,
                    Rules =
                    [
                        new RuleCreateRequest { DataPoint = "CreditScore", Condition = RuleCondition.GreaterOrEqualTo, Value = "700" },
                        new RuleCreateRequest { DataPoint = "DebtToIncomeRatio", Condition = RuleCondition.LessOrEqualTo, Value = "0.4" }
                    ]
                },
                new RuleGroupCreateRequest
                {
                    Name = "Moderate Credit Path",
                    Grouping = RuleGrouping.And,
                    Rules =
                    [
                        new RuleCreateRequest { DataPoint = "CreditScore", Condition = RuleCondition.GreaterOrEqualTo, Value = "600" }
                    ],
                    ChildGroups =
                    [
                        new RuleGroupCreateRequest
                        {
                            Name = "Secondary Conditions",
                            Grouping = RuleGrouping.Or,
                            Rules =
                            [
                                new RuleCreateRequest { DataPoint = "DebtToIncomeRatio", Condition = RuleCondition.LessOrEqualTo, Value = "0.3" },
                                new RuleCreateRequest { DataPoint = "DelinquencyStatus", Condition = RuleCondition.In, Value = "Current,Days30" }
                            ]
                        }
                    ]
                }
            ]
        };

        var result = RuleGroup.Create(request);
        result.IsSuccess.ShouldBeTrue();
        return result.Value!;
    }

    [Fact]
    public void Evaluate_HighCreditScoreAndLowDti_Passes()
    {
        var policy = BuildApprovalPolicy();
        // DTI = (3000 * 12) / 100000 = 0.36
        var application = LoanApplication.CreateForTesting(
            annualIncome: 100000,
            creditScore: 750,
            monthlyDebtPayments: 3000);

        var result = RuleEvaluator.Evaluate(policy, application);

        result.Passed.ShouldBeTrue();
    }

    [Fact]
    public void Evaluate_ModerateCreditScoreWithRecentDelinquency_Passes()
    {
        var policy = BuildApprovalPolicy();
        // DTI = (4000 * 12) / 100000 = 0.48 - too high on its own, but Days30 delinquency saves it
        var application = LoanApplication.CreateForTesting(
            annualIncome: 100000,
            creditScore: 650,
            monthlyDebtPayments: 4000,
            delinquencyStatus: Delinquency.Days30);

        var result = RuleEvaluator.Evaluate(policy, application);

        result.Passed.ShouldBeTrue();
    }

    [Fact]
    public void Evaluate_LowCreditScore_Fails()
    {
        var policy = BuildApprovalPolicy();
        var application = LoanApplication.CreateForTesting(
            annualIncome: 100000,
            creditScore: 500,
            monthlyDebtPayments: 1000,
            delinquencyStatus: Delinquency.Current);

        var result = RuleEvaluator.Evaluate(policy, application);

        result.Passed.ShouldBeFalse();
    }

    [Fact]
    public void Evaluate_ModerateCreditScoreWithHighDtiAndOldDelinquency_Fails()
    {
        var policy = BuildApprovalPolicy();
        var application = LoanApplication.CreateForTesting(
            annualIncome: 100000,
            creditScore: 650,
            monthlyDebtPayments: 4000,
            delinquencyStatus: Delinquency.Days60);

        var result = RuleEvaluator.Evaluate(policy, application);

        result.Passed.ShouldBeFalse();
    }

    [Fact]
    public void Evaluate_ProducesPerRuleAuditDetails()
    {
        var policy = BuildApprovalPolicy();
        var application = LoanApplication.CreateForTesting(
            annualIncome: 100000,
            creditScore: 750,
            monthlyDebtPayments: 3000);

        var result = RuleEvaluator.Evaluate(policy, application);

        var highCreditPath = result.ChildResults.Single(c => c.Name == "High Credit Path");
        highCreditPath.Passed.ShouldBeTrue();
        highCreditPath.RuleResults.ShouldContain(r => r.DataPoint == "CreditScore" && r.Passed && r.Details.Contains("750"));
        highCreditPath.RuleResults.ShouldContain(r => r.DataPoint == "DebtToIncomeRatio" && r.Passed && r.Details.Contains("0.36"));
    }
}
