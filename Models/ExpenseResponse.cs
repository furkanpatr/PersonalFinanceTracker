namespace PersonalFinanceTracker.Api.Models;

// API'den dışarı dönen harcama verisinin yapısını temsil eder.
public record ExpenseResponse(
    int Id,
    int UserId,
    int CategoryId,
    decimal Amount,
    DateOnly Date,
    string? Place,
    string? Description,
    int PaymentMethodId
);