namespace PersonalFinanceTracker.Api.Models;

public record IncomeRequest(
    int IncomeCategoryId,
    decimal Amount,
    DateOnly Date,
    string? Description
);