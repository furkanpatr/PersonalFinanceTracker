namespace PersonalFinanceTracker.Api.Models;

// API'ye yeni harcama ekleme veya güncelleme sırasında gelen veriyi temsil eder.
public record ExpenseRequest(
    int CategoryId,
    decimal Amount,
    DateOnly Date,
    string Place,
    string Description,
    int PaymentMethodId
);