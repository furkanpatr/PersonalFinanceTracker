# Integration test veritabani yoksa otomatik olusturulacak.
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($env:PFT_MIGRATIONS_CONNECTION)) {
    $line = dotnet user-secrets list --project PersonalFinanceTracker.Api.csproj |
    Where-Object { $_.StartsWith("ConnectionStrings:DefaultConnection = ") } |
    Select-Object -First 1

    if (-not $line) {
        throw "User Secrets baglantisi bulunamadi!"
    }

    $source = ($line -split " = ", 2)[1]

    if ($source -notmatch '(?i)(^|;)Database=expense_tracker(?=;|$)') {
        throw "Kaynak veritabani beklenenden farkli!"
    }

    $env:PFT_MIGRATIONS_CONNECTION = [regex]::Replace(
        $source,
        '(?i)(^|;)Database=expense_tracker(?=;|$)',
        '${1}Database=personal_finance_tracker_automated_test'
    )
}


$connection = $env:PFT_MIGRATIONS_CONNECTION

if ($connection -notmatch '(?i)(^|;)Database=personal_finance_tracker_automated_test(;|$)') {
    throw "Test veritabani baglantisi bulunamadi!"
}

dotnet run --project tools/PersonalFinanceTracker.Migrations/PersonalFinanceTracker.Migrations.csproj

if ($LASTEXITCODE -ne 0) {
    throw "Migration basarisiz oldu!"
}

dotnet test tests/PersonalFinanceTracker.Api.Tests/PersonalFinanceTracker.Api.Tests.csproj

if ($LASTEXITCODE -ne 0) {
    throw "Integration testleri basarisiz oldu!"
}
