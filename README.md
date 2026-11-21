# Personal Finance Tracker

A REST API for managing personal finances, built with **ASP.NET Core (.NET 10)** and **PostgreSQL**.

The project supports expense and income tracking, budget management, recurring payments, future payments, and financial reporting. Financial records are protected with JWT authentication and user-scoped access.

## Tech Stack

- **Backend:** C#, ASP.NET Core Minimal APIs (.NET 10)
- **Database:** PostgreSQL, Npgsql
- **Database Migrations:** DbUp
- **Authentication:** JWT, password hashing
- **Testing:** xUnit, ASP.NET Core Integration Testing

## Features

### Authentication
- User registration and login
- Password hashing and JWT authentication
- Protected, user-specific API endpoints

### Financial Management
- Expense and income CRUD operations
- Monthly category-based budget limits
- Validation and duplicate prevention

### Payment Management
- Recurring payments with weekly, monthly, and yearly schedules
- Automatic generation of due payments
- Future payment tracking, postponement, and payment history
- Transaction-based payment operations

### Financial Reports
- Income and expense summaries
- Category-based reports
- Weekly and monthly comparisons
- Custom date range reports
- Financial discipline score based on payment behavior

## API Overview

The main API routes are:

| Resource | Base Route |
|---|---|
| Authentication | `/api/auth` |
| Expenses | `/api/expenses` |
| Income | `/api/incomes` |
| Budget Limits | `/api/budget-limits` |
| Recurring Payments | `/api/recurring-payments` |
| Future Payments | `/api/future-payments` |
| Reports | `/api/reports` |

The API uses standard HTTP methods for CRUD operations and dedicated endpoints for actions such as paying, postponing, and processing recurring payments.

## Getting Started

### Requirements

- .NET 10 SDK
- PostgreSQL

### 1. Configure the application

Create a local PostgreSQL database named `expense_tracker`.

Configure the connection string and JWT secret using **.NET User Secrets**:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=expense_tracker;Username=postgres;Password=YOUR_PASSWORD" --project PersonalFinanceTracker.Api.csproj

dotnet user-secrets set "Jwt:Key" "YOUR_LONG_RANDOM_JWT_SECRET" --project PersonalFinanceTracker.Api.csproj
```

Replace the placeholder values with your own credentials and secret.

### 2. Apply database migrations

The project uses DbUp to manage PostgreSQL schema changes.

For a new local database, configure the migration connection and run:

```powershell
$env:PFT_MIGRATIONS_CONNECTION = "Host=localhost;Database=expense_tracker;Username=postgres;Password=YOUR_PASSWORD"

$env:PFT_ALLOW_ORIGINAL_DB = "YES"

dotnet run --project tools/PersonalFinanceTracker.Migrations/PersonalFinanceTracker.Migrations.csproj
```

The migration scripts create the required tables, constraints, and initial reference data. DbUp records applied migrations to prevent duplicate execution.

The migration runner currently supports local PostgreSQL and requires explicit authorization for the `expense_tracker` database.

### 3. Run the API

```powershell
dotnet run --project PersonalFinanceTracker.Api.csproj
```

## Automated Testing

The project includes **56 integration tests** covering authentication, financial records, budgets, payments, reporting, validation, and user-scoped access.

Tests use an isolated PostgreSQL database named `personal_finance_tracker_automated_test`.

To prepare the test database and run the full suite:

```powershell
.\tools\run-integration-tests.ps1
```

The script applies the database migrations and runs the integration tests. It can create the test database automatically when needed.

**Latest verified test result: 56 passed, 0 failed.**

## Security

- Passwords are securely hashed.
- JWT authentication protects private endpoints.
- Financial records are scoped to the authenticated user.
- Database queries use parameterized SQL.
- Credentials and JWT secrets are stored outside source control.

## Project Structure

```text
PersonalFinanceTracker/
├── Endpoints/
├── Models/
├── Database/
│   └── Migrations/
├── tests/
│   └── PersonalFinanceTracker.Api.Tests/
├── tools/
│   ├── PersonalFinanceTracker.Migrations/
│   └── run-integration-tests.ps1
├── Program.cs
└── PersonalFinanceTracker.Api.csproj
```

## Roadmap

- Docker support
- CI/CD with GitHub Actions
- AWS deployment
- Production security and monitoring
- Frontend application

## Project Status

**Backend development is ongoing.**

Core financial features, database migrations, and automated integration tests are implemented. Deployment and frontend development are planned for later stages.
