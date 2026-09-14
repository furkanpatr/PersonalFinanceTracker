namespace PersonalFinanceTracker.Api.Models;

public record FuturePaymentPostponeRequest(
    DateOnly NewPlannedDate
);