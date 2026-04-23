<#
.SYNOPSIS
  k6 test runner for LandlordApp load tests.

.PARAMETER Scenario
  Which scenario to run. Defaults to 01 (cache stampede).
  Options: 01, 02, 03, 04, 05, 06, 07, 08, 09, all

.PARAMETER BaseUrl
  API base URL. Defaults to http://localhost:5197

.PARAMETER AuthToken
  JWT bearer token for authenticated endpoints.

.EXAMPLE
  .\run.ps1                                          # run scenario 01
  .\run.ps1 -Scenario 01 -BaseUrl http://localhost:5197
  .\run.ps1 -AuthToken "eyJhbGciOiJIUzI1NiIs..."
#>
param(
    [string] $Scenario    = "01",
    [string] $BaseUrl     = "http://localhost:5197",
    [string] $AuthToken   = "",
    [string] $ApartmentId = "",
    [string] $SenderId    = "",
    [string] $ReceiverId  = ""
)

# ── Check k6 is installed ───────────────────────────────────────────────────
if (-not (Get-Command k6 -ErrorAction SilentlyContinue)) {
    Write-Host ""
    Write-Host "❌  k6 is not installed." -ForegroundColor Red
    Write-Host ""
    Write-Host "Install via winget:" -ForegroundColor Yellow
    Write-Host "    winget install k6 --source winget"
    Write-Host ""
    Write-Host "Or via Chocolatey:" -ForegroundColor Yellow
    Write-Host "    choco install k6"
    Write-Host ""
    Write-Host "Or download from: https://k6.io/docs/get-started/installation/"
    exit 1
}

# ── Resolve script directory ────────────────────────────────────────────────
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# ── Map scenario number to file ─────────────────────────────────────────────
$ScenarioMap = @{
    "01" = "scenarios\01_cache_stampede.js"
    "02" = "scenarios\02_role_upgrade_race.js"
    "03" = "scenarios\03_monri_idempotency.js"
    "04" = "scenarios\04_listing_load.js"
    "05" = "scenarios\05_signalr_broadcast.js"
    "06" = "scenarios\06_concurrent_applications.js"
    "07" = "scenarios\07_auth_rate_limit.js"
    "08" = "scenarios\08_available_slots_load.js"
    "09" = "scenarios\09_message_idempotency.js"
}

function Run-Scenario($file) {
    $fullPath = Join-Path $ScriptDir $file
    if (-not (Test-Path $fullPath)) {
        Write-Host "❌  Scenario file not found: $fullPath" -ForegroundColor Red
        return
    }

    $envArgs = @("-e", "BASE_URL=$BaseUrl")
    if ($AuthToken)   { $envArgs += @("-e", "AUTH_TOKEN=$AuthToken") }
    if ($ApartmentId) { $envArgs += @("-e", "APARTMENT_ID=$ApartmentId") }
    if ($SenderId)    { $envArgs += @("-e", "SENDER_ID=$SenderId") }
    if ($ReceiverId)  { $envArgs += @("-e", "RECEIVER_ID=$ReceiverId") }

    Write-Host ""
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "  Running: $file" -ForegroundColor Cyan
    Write-Host "  API    : $BaseUrl" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host ""

    & k6 run @envArgs $fullPath

    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "⚠️  k6 exited with code $LASTEXITCODE (thresholds may have failed)" -ForegroundColor Yellow
    }
}

# ── Execute ─────────────────────────────────────────────────────────────────
if ($Scenario -eq "all") {
    foreach ($entry in $ScenarioMap.GetEnumerator() | Sort-Object Key) {
        Run-Scenario $entry.Value
    }
} elseif ($ScenarioMap.ContainsKey($Scenario)) {
    Run-Scenario $ScenarioMap[$Scenario]
} else {
    Write-Host "❌  Unknown scenario: '$Scenario'" -ForegroundColor Red
    Write-Host "    Available: $($ScenarioMap.Keys -join ', '), all"
    exit 1
}
