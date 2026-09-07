namespace PersonalFinanceTracker.Api.Models;

public record FuturePaymentRequest(
    int CategoryId,
    decimal Amount,
    DateOnly PlannedDate,
    string Status,
    string? ImportanceLevel,
    int PaymentMethodId
);