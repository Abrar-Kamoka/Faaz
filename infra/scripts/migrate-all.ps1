<#
.SYNOPSIS
    Applies EF Core migrations for all Faaz services in the correct dependency order.
    Identity runs first (creates AspNetUsers), then Student and Consultant.

.PARAMETER ConnectionString
    Override the connection string. Defaults to localhost dev connection.

.EXAMPLE
    ./migrate-all.ps1
    ./migrate-all.ps1 -ConnectionString "Server=.;Database=FaazDb;Trusted_Connection=True;"
#>
param(
    [string]$ConnectionString = "Server=localhost,1433;Database=FaazDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;"
)

$env:ConnectionStrings__FaazDb = $ConnectionString
$root = Split-Path $PSScriptRoot -Parent | Split-Path -Parent

function Invoke-Migration {
    param([string]$ProjectPath, [string]$ServiceName)

    Write-Host "`n==> Migrating $ServiceName ..." -ForegroundColor Cyan
    $fullPath = Join-Path $root $ProjectPath

    dotnet ef database update `
        --project $fullPath `
        --connection $ConnectionString

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Migration failed for $ServiceName. Aborting."
        exit 1
    }
    Write-Host "==> $ServiceName migration complete." -ForegroundColor Green
}

# 1. Identity first — creates AspNetUsers table
Invoke-Migration "src/Services/Identity/Faaz.Services.Identity" "Faaz.Services.Identity"

# 2. Student — FKs to AspNetUsers
Invoke-Migration "src/Services/Student/Faaz.Services.Student" "Faaz.Services.Student"

# 3. Consultant — FKs to AspNetUsers
Invoke-Migration "src/Services/Consultant/Faaz.Services.Consultant" "Faaz.Services.Consultant"

Write-Host "`nAll migrations applied successfully." -ForegroundColor Green
