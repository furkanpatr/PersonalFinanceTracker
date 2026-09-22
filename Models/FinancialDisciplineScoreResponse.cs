namespace PersonalFinanceTracker.Api.Models;

public record FinancialDisciplineScoreResponse(
    int Score,
    int OnTimePayments,
    int LatePayments,
    int Postponements,
    int OverduePayments
);