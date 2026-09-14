namespace PersonalFinanceTracker.Api.Models;

public record RecurringPaymentRequest(
    int CategoryId,
    int PaymentMethodId,
    string Name,
    decimal Amount,
    string Frequency,
    DateOnly NextDueDate,
    string ImportanceLevel,
    string? Description
);