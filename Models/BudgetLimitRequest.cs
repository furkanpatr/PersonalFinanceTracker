namespace PersonalFinanceTracker.Api.Models;

public record BudgetLimitRequest(
    int CategoryId,
    decimal Amount,
    DateOnly Period
);