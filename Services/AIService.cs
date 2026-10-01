using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using LoanDecisionApi.Models.Domain;
using LoanDecisionApi.Models.DTO;

namespace LoanDecisionApi.Services;

public class AIService(HttpClient httpClient, IConfiguration configuration)
{
    private const string LoanApplicationInputSchema = """
    {
        "type": "object",
        "properties": {
            "ssn": { "type": ["string", "null"], "description": "9-digit SSN as a string, or null if unavailable." },
            "annualIncome": { "type": ["number", "null"], "description": "Annual income as a number, or null if unavailable." },
            "dateOfBirth": { "type": ["string", "null"], "format": "date", "description": "ISO 8601 date in YYYY-MM-DD format, or null if unavailable." },
            "monthlyDebtPayments": { "type": ["number", "null"], "description": "Monthly debt payments as a number, or null if unavailable." },
            "isIdentityVerified": { "type": ["boolean", "null"], "description": "true/false/null if the value is not available." },
            "isFraudRiskFlagged": { "type": ["boolean", "null"], "description": "true/false/null if the value is not available." },
            "isCreditFreezeFlagged": { "type": ["boolean", "null"], "description": "true/false/null if the value is not available." },
            "delinquencyStatus": { "type": ["string", "null"], "description": "Exact enum name: Current, Days30, Days60, Days90Plus; use null when unknown." }
        },
        "required": ["ssn", "annualIncome", "dateOfBirth", "monthlyDebtPayments", "isIdentityVerified", "isFraudRiskFlagged", "isCreditFreezeFlagged", "delinquencyStatus"]
    }
    """;

    public async Task<LoanApplicationEvaluationRequest> ParseLoanApplicationRequestAsync(LoanApplicationEvaluationTextRequest textRequest)
    {
        if (string.IsNullOrWhiteSpace(textRequest.Text))
        {
            return new LoanApplicationEvaluationRequest();
        }

        var apiKey = configuration["Anthropic:ApiKey"] ?? throw new InvalidOperationException("Configuration value 'Anthropic:ApiKey' is not configured.");

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        var payload = new
        {
            model = "claude-sonnet-5",
            max_tokens = 1024,
            tools = new[]
            {
                new
                {
                    name = "extract_loan_application",
                    description = "Extract loan application profile fields from the provided text.",
                    input_schema = JsonNode.Parse(LoanApplicationInputSchema)
                }
            },
            tool_choice = new { type = "tool", name = "extract_loan_application" },
            messages = new[]
            {
                new { role = "user", content = textRequest.Text }
            }
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Anthropic API request failed ({(int)response.StatusCode}): {responseBody}");
        }

        using var document = JsonDocument.Parse(responseBody);
        var toolInput = ExtractToolInput(document.RootElement);

        if (toolInput is null)
        {
            return new LoanApplicationEvaluationRequest();
        }

        var root = toolInput.Value;

        return new LoanApplicationEvaluationRequest
        {
            Ssn = root.TryGetProperty("ssn", out var ssn) ? ssn.ValueKind == JsonValueKind.Null || ssn.ValueKind == JsonValueKind.Undefined ? null : ssn.GetString() : null,
            AnnualIncome = root.TryGetProperty("annualIncome", out var annualIncome) && annualIncome.ValueKind != JsonValueKind.Null && annualIncome.ValueKind != JsonValueKind.Undefined ? annualIncome.GetDecimal() : null,
            DateOfBirth = ParseNullableDateOnly(root, "dateOfBirth"),
            MonthlyDebtPayments = root.TryGetProperty("monthlyDebtPayments", out var monthlyDebtPayments) && monthlyDebtPayments.ValueKind != JsonValueKind.Null && monthlyDebtPayments.ValueKind != JsonValueKind.Undefined ? monthlyDebtPayments.GetDecimal() : null,
            IsIdentityVerified = root.TryGetProperty("isIdentityVerified", out var isIdentityVerified) && isIdentityVerified.ValueKind != JsonValueKind.Null && isIdentityVerified.ValueKind != JsonValueKind.Undefined ? isIdentityVerified.GetBoolean() : null,
            IsFraudRiskFlagged = root.TryGetProperty("isFraudRiskFlagged", out var isFraudRiskFlagged) && isFraudRiskFlagged.ValueKind != JsonValueKind.Null && isFraudRiskFlagged.ValueKind != JsonValueKind.Undefined ? isFraudRiskFlagged.GetBoolean() : null,
            IsCreditFreezeFlagged = root.TryGetProperty("isCreditFreezeFlagged", out var isCreditFreezeFlagged) && isCreditFreezeFlagged.ValueKind != JsonValueKind.Null && isCreditFreezeFlagged.ValueKind != JsonValueKind.Undefined ? isCreditFreezeFlagged.GetBoolean() : null,
            DelinquencyStatus = ParseNullableDelinquency(root, "delinquencyStatus")
        };
    }

    private static JsonElement? ExtractToolInput(JsonElement root)
    {
        if (root.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in content.EnumerateArray())
            {
                if (item.TryGetProperty("type", out var type) && type.GetString() == "tool_use" &&
                    item.TryGetProperty("input", out var input))
                {
                    return input;
                }
            }
        }

        return null;
    }

    private static DateOnly? ParseNullableDateOnly(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        var dateString = value.GetString();
        if (string.IsNullOrWhiteSpace(dateString) || dateString.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (DateOnly.TryParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            return parsedDate;
        }

        return DateOnly.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate)
            ? parsedDate
            : null;
    }

    private static Delinquency? ParseNullableDelinquency(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        var statusText = value.GetString();
        if (string.IsNullOrWhiteSpace(statusText) || statusText.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (Enum.TryParse<Delinquency>(statusText, true, out var delinquency) && Enum.IsDefined(delinquency))
        {
            return delinquency;
        }

        return null;
    }
}
