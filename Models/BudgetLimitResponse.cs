namespace PersonalFinanceTracker.Api.Models;

public record BudgetLimitResponse(
    int Id,
    int UserId,
    int CategoryId,
    decimal Amount,
    DateOnly Period
);