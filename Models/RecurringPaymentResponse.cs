namespace PersonalFinanceTracker.Api.Models;

public record RecurringPaymentResponse(
    int Id,
    int CategoryId,
    int PaymentMethodId,
    string Name,
    decimal Amount,
    string Frequency,
    DateOnly NextDueDate,
    string ImportanceLevel,
    bool IsActive,
    string? Description
);