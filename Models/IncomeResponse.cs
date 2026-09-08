namespace PersonalFinanceTracker.Api.Models;

public record IncomeResponse(
    int Id,
    int UserId,
    int IncomeCategoryId,
    decimal Amount,
    DateOnly Date,
    string? Description
);