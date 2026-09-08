# Personal Finance Tracker

A backend API project for managing personal expenses, income, and future payments.

The project is built with ASP.NET Core and PostgreSQL and focuses on secure, user-scoped financial data management.

## Technologies

- ASP.NET Core
- C#
- PostgreSQL
- Npgsql
- REST API
- JWT Authentication

## Current Features

### Authentication

- User registration
- User login
- Password hashing
- JWT authentication
- Protected API endpoints

### Expenses

- Get all expenses
- Get expense by id
- Create expense
- Update expense
- Delete expense
- User-scoped expense access
- Input validation
- Error handling

### Income Tracking

- Get all income records
- Create income
- Update income
- Delete income
- Income category support
- User-scoped income access
- Input validation
- Foreign key error handling

### Future Payments

- Get all future payments
- Get future payment by id
- Create future payment
- Update future payment
- Delete future payment
- User-scoped future payment access
- Payment status support
- Importance level support
- Input validation
- Foreign key error handling

### Reports

- Financial summary
- Category-based expense reports
- Monthly expense comparison
- Weekly expense comparison
- Future payment status reports
- Custom date range reports

## Security

- Passwords are stored as hashes
- JWT tokens are used for authentication
- User IDs are obtained from authenticated JWT claims
- Users can only access their own expenses, income records, and future payments
- SQL queries use parameters to reduce SQL injection risk
- Database connection strings and JWT secrets are kept outside source control

## API Endpoints

### Authentication

```text
POST /api/auth/register
POST /api/auth/login
```

### Expenses

```text
GET    /api/expenses
GET    /api/expenses/{id}
POST   /api/expenses
PUT    /api/expenses/{id}
DELETE /api/expenses/{id}
```

### Income

```text
GET    /api/incomes
POST   /api/incomes
PUT    /api/incomes/{id}
DELETE /api/incomes/{id}
```

### Future Payments

```text
GET    /api/future-payments
GET    /api/future-payments/{id}
POST   /api/future-payments
PUT    /api/future-payments/{id}
DELETE /api/future-payments/{id}
```

### Reports

```text
GET /api/reports/summary
GET /api/reports/categories
GET /api/reports/monthly
GET /api/reports/weekly
GET /api/reports/future-payments/status
GET /api/reports/range?startDate=YYYY-MM-DD&endDate=YYYY-MM-DD
```

## Project Structure

```text
PersonalFinanceTracker.Api/
├── Endpoints/
│   ├── AuthEndpoints.cs
│   ├── ExpenseEndpoints.cs
│   ├── FuturePaymentEndpoints.cs
│   ├── IncomeEndpoints.cs
│   └── ReportEndpoints.cs
│
├── Models/
│   ├── ExpenseRequest.cs
│   ├── ExpenseResponse.cs
│   ├── FuturePaymentRequest.cs
│   ├── FuturePaymentResponse.cs
│   ├── IncomeRequest.cs
│   ├── IncomeResponse.cs
│   ├── LoginRequest.cs
│   └── RegisterRequest.cs
│
├── Program.cs
└── appsettings.json
```

## Authentication Flow

1. A user registers with an email and password.
2. The password is hashed before being stored in PostgreSQL.
3. The user logs in with their credentials.
4. The API creates a JWT token.
5. The client sends the token with protected requests.
6. The API reads the user ID from the JWT claim.
7. Database queries are scoped to the authenticated user.

Example:

```text
Authorization: Bearer <token>
```

## Local Development

The project requires:

- .NET 10 SDK
- PostgreSQL

Sensitive configuration such as the database connection string and JWT secret should not be stored directly in source code.

For local development, .NET User Secrets can be used.

Required configuration keys:

```text
ConnectionStrings:DefaultConnection
Jwt:Key
```

Run the API with:

```bash
dotnet run
```

## Database Setup

This project uses PostgreSQL.

Before running the API, create a PostgreSQL database and store the connection string using .NET User Secrets.

Example:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_CONNECTION_STRING"
```

The database schema is currently created manually.

Migration support will be added in a future update.

## Planned Features

- Category-based budget limits
- Recurring payments
- Financial discipline score
- Automated tests
- Database setup scripts and migration support
- Docker support
- AWS deployment
- CI/CD
- Logging and monitoring
- Production security improvements
- Frontend/demo application
