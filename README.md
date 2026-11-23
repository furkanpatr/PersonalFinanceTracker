# Personal Finance Tracker

A REST API for personal finance management, built with **ASP.NET Core (.NET 10)** and **PostgreSQL**.

The application supports expense tracking, income management, monthly budgets, recurring payments, future payments, and financial reporting.

## Tech Stack

- **Backend:** C#, ASP.NET Core Minimal APIs
- **Database:** PostgreSQL, Npgsql
- **Migrations:** DbUp
- **Authentication:** JWT
- **Testing:** xUnit, ASP.NET Core Integration Testing

## Features

### Authentication
- User registration and login
- Password hashing
- JWT authentication and user-scoped access

### Financial Management
- Expense and income CRUD operations
- Category-based monthly budget limits
- Input validation and duplicate prevention

### Payment Management
- Weekly, monthly, and yearly recurring payments
- Automatic generation of due payments
- Future payment tracking, postponement, and payment history
- Transaction-based payment operations
- PostgreSQL row-level locking for payment and postponement operations
- Duplicate payment processing protection
- Month-end date handling for recurring payments
- Leap-year-aware recurring payment scheduling

### Financial Reports
- Income and expense summaries
- Category-based reports
- Weekly and monthly comparisons
- Custom date range reports
- Financial discipline score

## API Overview

| Resource | Base Route |
|---|---|
| Authentication | `/api/auth` |
| Expenses | `/api/expenses` |
| Income | `/api/incomes` |
| Budget Limits | `/api/budget-limits` |
| Recurring Payments | `/api/recurring-payments` |
| Future Payments | `/api/future-payments` |
| Reports | `/api/reports` |

The API provides standard CRUD operations and dedicated endpoints for payment processing, postponement, recurring payments, and financial reports.

## Getting Started

### Requirements

- .NET 10 SDK
- PostgreSQL

### 1. Configure the application

Create a local PostgreSQL database named `expense_tracker`.

Store the connection string and JWT secret using .NET User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=expense_tracker;Username=postgres;Password=YOUR_PASSWORD" --project PersonalFinanceTracker.Api.csproj

dotnet user-secrets set "Jwt:Key" "YOUR_LONG_RANDOM_JWT_SECRET" --project PersonalFinanceTracker.Api.csproj
```

Replace the placeholder values with your own credentials and secret.

### 2. Apply database migrations

Database migrations are managed using **DbUp**.

```powershell
$env:PFT_MIGRATIONS_CONNECTION = "Host=localhost;Database=expense_tracker;Username=postgres;Password=YOUR_PASSWORD"

$env:PFT_ALLOW_ORIGINAL_DB = "YES"

dotnet run --project tools/PersonalFinanceTracker.Migrations/PersonalFinanceTracker.Migrations.csproj
```

Migration files:

- `001_initial_schema.sql` — Database tables and constraints
- `002_seed_reference_data.sql` — Initial categories and payment methods
- `003_recurring_payment_schedule_day.sql` — Recurring payment schedule day support

Applied migrations are tracked automatically to prevent duplicate execution.

The migration runner currently supports local PostgreSQL and requires explicit authorization for the `expense_tracker` database.

### 3. Run the API

```powershell
dotnet run --project PersonalFinanceTracker.Api.csproj
```

## Automated Testing

The project includes **64 integration tests** covering:

- Authentication and authorization
- Expense, income, and budget operations
- Recurring and future payments
- Concurrent payment requests
- Payment postponement consistency
- Month-end and leap-year date calculations
- Financial reports and validation
- User-scoped data access

Tests use an isolated PostgreSQL database named `personal_finance_tracker_automated_test`.

Run the complete test suite:

```powershell
.\tools\run-integration-tests.ps1
```

The script prepares the test database, applies migrations, and executes the integration tests.

**Latest verified result: 64 passed, 0 failed.**

## Security

- Hashed user passwords
- JWT-protected endpoints
- User-scoped financial records
- Parameterized SQL queries
- Transaction-based payment operations
- PostgreSQL row-level locking for critical payment actions
- Secrets and connection strings stored outside source control

## Project Structure

```text
PersonalFinanceTracker/
├── Endpoints/
├── Models/
├── Database/
│   └── Migrations/
│       ├── 001_initial_schema.sql
│       ├── 002_seed_reference_data.sql
│       └── 003_recurring_payment_schedule_day.sql
├── tests/
│   └── PersonalFinanceTracker.Api.Tests/
├── tools/
│   ├── PersonalFinanceTracker.Migrations/
│   └── run-integration-tests.ps1
├── Program.cs
└── PersonalFinanceTracker.Api.csproj
```

## Roadmap

- Code refactoring and API consistency
- Docker support
- Production security and logging
- CI/CD with GitHub Actions
- AWS deployment
- Monitoring and health checks
- Frontend application

## Project Status

**Backend development is ongoing.**

Core financial features, PostgreSQL migrations, payment consistency improvements, recurring payment date handling, and automated integration tests are implemented.

Containerization, cloud deployment, and frontend development are planned for upcoming stages.
