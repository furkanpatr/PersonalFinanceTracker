namespace PersonalFinanceTracker.Api.Models;

public record RegisterRequest(
    string Name,
    string Surname,
    string? Phone,
    string Email,
    string Password
);