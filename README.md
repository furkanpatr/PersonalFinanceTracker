# Personal Finance Tracker

A REST API for personal finance management, built with **ASP.NET Core (.NET 10)** and **PostgreSQL**.

The application supports expense tracking, income management, monthly budgets, recurring payments, future payments, and financial reporting.

## Tech Stack

- **Backend:** C#, ASP.NET Core Minimal APIs
- **Database:** PostgreSQL, Npgsql
- **Migrations:** DbUp
- **Authentication:** JWT
- **Testing:** xUnit, ASP.NET Core Integration Testing
- **Containerization:** Docker, Docker Compose

## Features

### Authentication
- User registration and login
- Password hashing
- JWT authentication and user-scoped access
- Centralized JWT user ID extraction using a reusable helper

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


The migration runner supports both local PostgreSQL and the Docker Compose PostgreSQL service. Docker access requires explicit authorization through PFT_ALLOW_DOCKER_DB, while migrations targeting expense_tracker require PFT_ALLOW_ORIGINAL_DB.

### 3. Run the API

```powershell
dotnet run --project PersonalFinanceTracker.Api.csproj
```

## Docker Setup

The application supports containerized deployment using Docker Compose.

### Requirements

- Docker Desktop
- Docker Compose
- A password-protected Data Protection certificate at `.secrets/dataprotection.pfx`

The certificate must be created locally and is excluded from Git and Docker build contexts.

### Create the Data Protection Certificate

Run the following commands in PowerShell to create a local, password-protected X.509 certificate:

```powershell
New-Item -ItemType Directory -Path ".secrets" -Force | Out-Null

$dpCert = New-SelfSignedCertificate -Type DocumentEncryptionCert -Subject "CN=PersonalFinanceTracker DataProtection" -CertStoreLocation "Cert:\CurrentUser\My" -KeyExportPolicy Exportable -KeyAlgorithm RSA -KeyLength 3072 -HashAlgorithm SHA256 -NotAfter (Get-Date).AddYears(2)

$pfxPassword = Read-Host "PFX certificate password" -AsSecureString

Export-PfxCertificate -Cert $dpCert -FilePath ".secrets/dataprotection.pfx" -Password $pfxPassword
```

Store the PFX password securely and restrict access to the `.secrets` directory.

Back up the certificate and password securely. Existing encrypted Data Protection keys may become unreadable if the original certificate or its private key is lost.

Never commit the PFX file, its password, or Data Protection key backups to Git.

### Configure Environment Variables

Set the following variables in the same PowerShell terminal:

```powershell
$env:PFT_POSTGRES_PASSWORD = [System.Net.NetworkCredential]::new("", (Read-Host "PostgreSQL password" -AsSecureString)).Password

$env:PFT_JWT_KEY = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))

$env:PFT_DP_CERT_PASSWORD = [System.Net.NetworkCredential]::new("", (Read-Host "PFX certificate password" -AsSecureString)).Password
```

The PFX password must match the certificate stored in `.secrets/dataprotection.pfx`.

### Start the Application

```powershell
docker compose up -d --build api
```

Docker Compose starts PostgreSQL, applies database migrations, and starts the API.

API address: `http://localhost:8080`

### Check Container Status

```powershell
docker compose ps -a
```

### View API Logs

```powershell
docker compose logs --tail 30 api
```

### Stop the Application

```powershell
docker compose down
```

Named Docker volumes preserve database records and Data Protection keys between container restarts.

Do not use `docker compose down -v` unless you intentionally want to remove the associated persistent volumes.

### Security Notes

- PostgreSQL is not exposed to the host.
- The API is bound to `127.0.0.1:8080`.
- The API runs as a non-root user.
- Data Protection keys are stored in a persistent volume.
- Newly generated Data Protection keys are protected using an X.509 certificate.
- Local certificates and private secrets are excluded from source control.

## Automated Testing

The project includes **65 integration tests** covering:

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

**Latest verified result: 65 passed, 0 failed.**

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
├── Helpers/
│   └── UserIdHelper.cs
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
├── PersonalFinanceTracker.Api.csproj
├── Dockerfile
├── Dockerfile.migrations
├── compose.yaml
└── .dockerignore

```

## Roadmap

- Production security and logging
- CI/CD with GitHub Actions
- AWS deployment
- Monitoring and health checks
- Frontend application

## Project Status

**Backend development is ongoing.**

Core financial features, PostgreSQL migrations, payment consistency improvements, recurring payment date handling, and automated integration tests are implemented.

Docker Compose containerization has been implemented and tested locally. Cloud deployment and frontend development are planned for upcoming stages.
