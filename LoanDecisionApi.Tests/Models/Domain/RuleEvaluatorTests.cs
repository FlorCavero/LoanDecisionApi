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

        result.Outcome.ShouldBe(EvaluationOutcome.Passed);
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

        result.Outcome.ShouldBe(EvaluationOutcome.Passed);
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

        result.Outcome.ShouldBe(EvaluationOutcome.Failed);
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

        result.Outcome.ShouldBe(EvaluationOutcome.Failed);
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
        highCreditPath.Outcome.ShouldBe(EvaluationOutcome.Passed);
        highCreditPath.RuleResults.ShouldContain(r => r.DataPoint == "CreditScore" && r.Outcome == EvaluationOutcome.Passed && r.Details.Contains("750"));
        highCreditPath.RuleResults.ShouldContain(r => r.DataPoint == "DebtToIncomeRatio" && r.Outcome == EvaluationOutcome.Passed && r.Details.Contains("0.36"));
    }

    [Fact]
    public void EvaluateRule_UnknownDataPoint_IsSkippedNotFailed()
    {
        var group = RuleGroup.Create(new RuleGroupCreateRequest
        {
            Name = "Fraud Gate",
            Grouping = RuleGrouping.And,
            Rules = [new RuleCreateRequest { DataPoint = "IsFraudRiskFlagged", Condition = RuleCondition.Equals, Value = "false" }]
        }).Value!;

        // IsFraudRiskFlagged left as its default (null) - fraud check hasn't run yet.
        var application = LoanApplication.CreateForTesting(creditScore: 700);

        var result = RuleEvaluator.Evaluate(group, application);

        result.RuleResults.Single().Outcome.ShouldBe(EvaluationOutcome.Skipped);
        result.Outcome.ShouldBe(EvaluationOutcome.Skipped);
    }

    [Fact]
    public void Evaluate_AndGroup_SkippedRuleDoesNotOverrideAnAlreadyFailedRule()
    {
        var group = RuleGroup.Create(new RuleGroupCreateRequest
        {
            Name = "And Gate",
            Grouping = RuleGrouping.And,
            Rules =
            [
                new RuleCreateRequest { DataPoint = "CreditScore", Condition = RuleCondition.GreaterOrEqualTo, Value = "700" }, // fails: 500 < 700
                new RuleCreateRequest { DataPoint = "IsFraudRiskFlagged", Condition = RuleCondition.Equals, Value = "false" }   // skipped: unknown
            ]
        }).Value!;

        var application = LoanApplication.CreateForTesting(creditScore: 500);

        var result = RuleEvaluator.Evaluate(group, application);

        result.Outcome.ShouldBe(EvaluationOutcome.Failed);
    }

    [Fact]
    public void Evaluate_AndGroup_SkippedRuleMakesAnOtherwisePassingGroupIndeterminate()
    {
        var group = RuleGroup.Create(new RuleGroupCreateRequest
        {
            Name = "And Gate",
            Grouping = RuleGrouping.And,
            Rules =
            [
                new RuleCreateRequest { DataPoint = "CreditScore", Condition = RuleCondition.GreaterOrEqualTo, Value = "700" }, // passes: 750 >= 700
                new RuleCreateRequest { DataPoint = "IsFraudRiskFlagged", Condition = RuleCondition.Equals, Value = "false" }   // skipped: unknown
            ]
        }).Value!;

        var application = LoanApplication.CreateForTesting(creditScore: 750);

        var result = RuleEvaluator.Evaluate(group, application);

        result.Outcome.ShouldBe(EvaluationOutcome.Skipped);
    }

    [Fact]
    public void Evaluate_OrGroup_SkippedRuleDoesNotOverrideAnAlreadyPassedRule()
    {
        var group = RuleGroup.Create(new RuleGroupCreateRequest
        {
            Name = "Or Gate",
            Grouping = RuleGrouping.Or,
            Rules =
            [
                new RuleCreateRequest { DataPoint = "CreditScore", Condition = RuleCondition.GreaterOrEqualTo, Value = "700" }, // passes: 750 >= 700
                new RuleCreateRequest { DataPoint = "IsFraudRiskFlagged", Condition = RuleCondition.Equals, Value = "false" }   // skipped: unknown
            ]
        }).Value!;

        var application = LoanApplication.CreateForTesting(creditScore: 750);

        var result = RuleEvaluator.Evaluate(group, application);

        result.Outcome.ShouldBe(EvaluationOutcome.Passed);
    }

    [Fact]
    public void Evaluate_OrGroup_SkippedRuleMakesAnOtherwiseFailingGroupIndeterminate()
    {
        var group = RuleGroup.Create(new RuleGroupCreateRequest
        {
            Name = "Or Gate",
            Grouping = RuleGrouping.Or,
            Rules =
            [
                new RuleCreateRequest { DataPoint = "CreditScore", Condition = RuleCondition.GreaterOrEqualTo, Value = "700" }, // fails: 500 < 700
                new RuleCreateRequest { DataPoint = "IsFraudRiskFlagged", Condition = RuleCondition.Equals, Value = "false" }   // skipped: unknown
            ]
        }).Value!;

        var application = LoanApplication.CreateForTesting(creditScore: 500);

        var result = RuleEvaluator.Evaluate(group, application);

        result.Outcome.ShouldBe(EvaluationOutcome.Skipped);
    }
}
