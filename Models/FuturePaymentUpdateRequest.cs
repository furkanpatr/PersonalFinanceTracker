namespace PersonalFinanceTracker.Api.Models;

public record FuturePaymentUpdateRequest(
    int CategoryId,
    decimal Amount,
    string ImportanceLevel,
    int PaymentMethodId
);