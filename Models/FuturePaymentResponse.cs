namespace PersonalFinanceTracker.Api.Models;

public record FuturePaymentResponse(
    int Id,
    int UserId,
    int CategoryId,
    decimal Amount,
    DateOnly PlannedDate,
    string Status,
    string? ImportanceLevel,
    int PaymentMethodId
);