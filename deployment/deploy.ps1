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

function Test-GCloudResource {
    param([scriptblock]$Command)
    $ErrorActionPreference = "Continue"
    & $gcloud @Command 2>$null | Out-Null
    $exists = $LASTEXITCODE -eq 0
    $ErrorActionPreference = "Stop"
    return $exists
}

# ---------------------------------------------------------------------------
# Load secrets
# ---------------------------------------------------------------------------
$secretsFile = Join-Path $rootPath "secrets/deploy_secrets.json"
if (-not (Test-Path $secretsFile)) {
    Write-Error "secrets/deploy_secrets.json not found. Copy secrets/deploy_secrets.template.json to secrets/deploy_secrets.json and fill in the values."
}

$secrets = Get-Content $secretsFile -Raw | ConvertFrom-Json
$neonConnStr      = $secrets.NeonConnectionString
$gcpProjectId     = $secrets.GcpProjectId
$gcpRegion        = $secrets.GcpRegion
$firebaseProjectId = $secrets.FirebaseProjectId
$frontendUrl      = $secrets.FrontendUrl

if ([string]::IsNullOrWhiteSpace($neonConnStr))       { Write-Error "NeonConnectionString is not set in secrets.json." }
if ([string]::IsNullOrWhiteSpace($gcpProjectId))      { Write-Error "GcpProjectId is not set in secrets.json." }
if ([string]::IsNullOrWhiteSpace($gcpRegion))         { Write-Error "GcpRegion is not set in secrets.json." }
if ([string]::IsNullOrWhiteSpace($firebaseProjectId)) { Write-Error "FirebaseProjectId is not set in secrets.json." }
if ([string]::IsNullOrWhiteSpace($frontendUrl))       { Write-Error "FrontendUrl is not set in secrets.json." }

$imageTag        = "$gcpRegion-docker.pkg.dev/$gcpProjectId/apotheca/api:latest"
$cloudRunService = "apotheca-api"
$saName          = "apotheca-api"
$saEmail         = "$saName@$gcpProjectId.iam.gserviceaccount.com"
$dbSecretName    = "apotheca-db-connection-string"

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
    "secretmanager.googleapis.com",
    "artifactregistry.googleapis.com",
    "cloudbuild.googleapis.com",
    "run.googleapis.com"
)

foreach ($api in $requiredApis) {
    Invoke-GCloud services enable $api --project=$gcpProjectId | Out-Null
    Write-Host "    Enabled: $api" -ForegroundColor Green
}

# ---------------------------------------------------------------------------
# Phase 1: Run database migrations against Neon
# ---------------------------------------------------------------------------
Write-Host ""
$runMigrations = Read-Host "==> Run database migrations? (Y/N)"
if ($runMigrations -eq 'Y' -or $runMigrations -eq 'y') {
    Write-Host "    Running migrations..." -ForegroundColor Cyan
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
} else {
    Write-Host "    Skipping migrations." -ForegroundColor Gray
}

# ---------------------------------------------------------------------------
# Phase 2: Sync DB connection string to Secret Manager
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "==> Syncing connection string to Secret Manager..." -ForegroundColor Cyan

if (-not (Test-GCloudResource { "secrets", "describe", $dbSecretName, "--project=$gcpProjectId" })) {
    Write-Host "    Creating secret '$dbSecretName'..." -ForegroundColor Yellow
    Invoke-GCloud secrets create $dbSecretName `
        --project=$gcpProjectId `
        --replication-policy="automatic"
}

$tmpFile = [System.IO.Path]::GetTempFileName()
try {
    [System.IO.File]::WriteAllText($tmpFile, $neonConnStr, [System.Text.UTF8Encoding]::new($false))
    Invoke-GCloud secrets versions add $dbSecretName `
        --project=$gcpProjectId `
        --data-file=$tmpFile
} finally {
    Remove-Item $tmpFile -ErrorAction SilentlyContinue
}

Write-Host "    Secret '$dbSecretName' updated." -ForegroundColor Green

# ---------------------------------------------------------------------------
# Phase 3: Build and push API image via Cloud Build
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "==> Building and pushing API image..." -ForegroundColor Cyan
Write-Host "    Image: $imageTag" -ForegroundColor Gray

# Create Artifact Registry repository if it doesn't exist
if (-not (Test-GCloudResource { "artifacts", "repositories", "describe", "apotheca", "--location=$gcpRegion", "--project=$gcpProjectId" })) {
    Write-Host "    Creating Artifact Registry repository 'apotheca'..." -ForegroundColor Yellow
    Invoke-GCloud artifacts repositories create apotheca `
        --repository-format=docker `
        --location=$gcpRegion `
        --project=$gcpProjectId
}

# Build and push using Cloud Build (no local Docker required)
Push-Location $rootPath
try {
    Invoke-GCloud builds submit . `
        --tag=$imageTag `
        --project=$gcpProjectId
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Cloud Build failed."
    }
} finally {
    Pop-Location
}

Write-Host "    Image built and pushed successfully." -ForegroundColor Green

# ---------------------------------------------------------------------------
# Phase 4: Deploy to Cloud Run
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "==> Deploying to Cloud Run..." -ForegroundColor Cyan

# Create dedicated service account if it doesn't exist
if (-not (Test-GCloudResource { "iam", "service-accounts", "describe", $saEmail, "--project=$gcpProjectId" })) {
    Write-Host "    Creating service account '$saName'..." -ForegroundColor Yellow
    Invoke-GCloud iam service-accounts create $saName `
        --project=$gcpProjectId `
        --display-name="Apotheca API"
}

# Grant the service account access to the DB secret
Invoke-GCloud secrets add-iam-policy-binding $dbSecretName `
    --member="serviceAccount:$saEmail" `
    --role="roles/secretmanager.secretAccessor" `
    --project=$gcpProjectId | Out-Null
Write-Host "    Granted Secret Manager access to service account." -ForegroundColor Green

# Deploy the Cloud Run service
Invoke-GCloud run deploy $cloudRunService `
    --image=$imageTag `
    --region=$gcpRegion `
    --platform=managed `
    --service-account=$saEmail `
    --set-secrets="ConnectionStrings__Postgres=$dbSecretName`:latest" `
    --set-env-vars="Firebase__ProjectId=$firebaseProjectId,Cors__AllowedOrigins__0=$frontendUrl" `
    --allow-unauthenticated `
    --project=$gcpProjectId

if ($LASTEXITCODE -ne 0) {
    Write-Error "Cloud Run deployment failed."
}

# Retrieve and display the deployed service URL
$serviceUrl = & $gcloud run services describe $cloudRunService `
    --region=$gcpRegion `
    --project=$gcpProjectId `
    --format="value(status.url)" 2>$null

Write-Host "    Deployed successfully." -ForegroundColor Green
Write-Host "    Service URL: $serviceUrl" -ForegroundColor Green

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "==> Deployment complete." -ForegroundColor Green
Write-Host ""
