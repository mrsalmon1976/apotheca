$ErrorActionPreference = "Stop"
Clear-Host

$scriptPath = $PSScriptRoot
$rootPath = Resolve-Path "$scriptPath/.."
$sourcePath = Join-Path $rootPath "source"
$migrationsProject = Join-Path $sourcePath "web-api/Apotheca.Migrations"

# ---------------------------------------------------------------------------
# Locate gcloud — on Windows it installs as gcloud.cmd and may not be on PATH
# ---------------------------------------------------------------------------
$gcloud = Get-Command gcloud -ErrorAction SilentlyContinue |
          Select-Object -ExpandProperty Source

if (-not $gcloud) {
    $candidates = @(
        "$env:LOCALAPPDATA\Google\Cloud SDK\google-cloud-sdk\bin\gcloud.cmd",
        "$env:ProgramFiles\Google\Cloud SDK\google-cloud-sdk\bin\gcloud.cmd",
        "${env:ProgramFiles(x86)}\Google\Cloud SDK\google-cloud-sdk\bin\gcloud.cmd"
    )
    $gcloud = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $gcloud) {
    Write-Error "gcloud not found. Install the Google Cloud SDK from https://cloud.google.com/sdk/docs/install and re-run."
}

function Invoke-GCloud {
    & $gcloud @args
}

# ---------------------------------------------------------------------------
# Load secrets
# ---------------------------------------------------------------------------
$secretsFile = Join-Path $scriptPath "secrets.json"
if (-not (Test-Path $secretsFile)) {
    Write-Error "secrets.json not found. Copy secrets.template.json to secrets.json and fill in the values."
}

$secrets = Get-Content $secretsFile -Raw | ConvertFrom-Json
$neonConnStr   = $secrets.NeonConnectionString
$gcpProjectId  = $secrets.GcpProjectId
$gcpRegion     = $secrets.GcpRegion

if ([string]::IsNullOrWhiteSpace($neonConnStr))  { Write-Error "NeonConnectionString is not set in secrets.json." }
if ([string]::IsNullOrWhiteSpace($gcpProjectId)) { Write-Error "GcpProjectId is not set in secrets.json." }
if ([string]::IsNullOrWhiteSpace($gcpRegion))    { Write-Error "GcpRegion is not set in secrets.json." }

# ---------------------------------------------------------------------------
# Validate gcloud auth
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "==> Checking gcloud authentication..." -ForegroundColor Cyan
$account = & $gcloud auth list --format="value(account)" 2>$null | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($account)) {
    Write-Error "No active gcloud account found. Run 'gcloud auth login' first, then re-run this script."
}
Write-Host "    Authenticated as: $account" -ForegroundColor Green

Invoke-GCloud config set project $gcpProjectId | Out-Null
Write-Host "    Project set to:   $gcpProjectId" -ForegroundColor Green

# ---------------------------------------------------------------------------
# Enable required GCP APIs
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "==> Enabling required GCP APIs..." -ForegroundColor Cyan

$requiredApis = @(
    "secretmanager.googleapis.com"
)

foreach ($api in $requiredApis) {
    Invoke-GCloud services enable $api --project=$gcpProjectId | Out-Null
    Write-Host "    Enabled: $api" -ForegroundColor Green
}

# ---------------------------------------------------------------------------
# Phase 1: Run database migrations against Neon
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "==> Running database migrations..." -ForegroundColor Cyan

$env:ConnectionString = $neonConnStr

try {
    dotnet run --project $migrationsProject --configuration Release --no-launch-profile
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Migrations failed (exit code $LASTEXITCODE)."
    }
    Write-Host "    Migrations applied successfully." -ForegroundColor Green
}
finally {
    Remove-Item Env:\ConnectionString -ErrorAction SilentlyContinue
}

# ---------------------------------------------------------------------------
# Phase 2: Publish connection string to GCP Secret Manager
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "==> Syncing Neon connection string to Secret Manager..." -ForegroundColor Cyan

$secretName = "apotheca-db-connection-string"

$ErrorActionPreference = "Continue"
& $gcloud secrets describe $secretName --project=$gcpProjectId 2>$null | Out-Null
$secretExists = $LASTEXITCODE -eq 0
$ErrorActionPreference = "Stop"

if (-not $secretExists) {
    Write-Host "    Creating secret '$secretName'..." -ForegroundColor Yellow
    Invoke-GCloud secrets create $secretName `
        --project=$gcpProjectId `
        --replication-policy="automatic"
}

$tmpFile = [System.IO.Path]::GetTempFileName()
try {
    [System.IO.File]::WriteAllText($tmpFile, $neonConnStr, [System.Text.Encoding]::UTF8)
    Invoke-GCloud secrets versions add $secretName `
        --project=$gcpProjectId `
        --data-file=$tmpFile
} finally {
    Remove-Item $tmpFile -ErrorAction SilentlyContinue
}

Write-Host "    Secret '$secretName' updated." -ForegroundColor Green

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "==> Deployment phase 1 (database) complete." -ForegroundColor Green
Write-Host ""
