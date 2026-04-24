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
$neonConnStr             = $secrets.NeonConnectionString
$gcpProjectId            = $secrets.GcpProjectId
$gcpRegion               = $secrets.GcpRegion
$firebaseProjectId       = $secrets.FirebaseProjectId
$frontendUrl             = $secrets.FrontendUrl
$viteApiUrl              = $secrets.ViteApiUrl
$viteFirebaseApiKey      = $secrets.ViteFirebaseApiKey
$viteFirebaseAuthDomain  = $secrets.ViteFirebaseAuthDomain
$viteFirebaseProjectId   = $secrets.ViteFirebaseProjectId
$viteFirebaseStorageBucket     = $secrets.ViteFirebaseStorageBucket
$viteFirebaseMessagingSenderId = $secrets.ViteFirebaseMessagingSenderId
$viteFirebaseAppId       = $secrets.ViteFirebaseAppId
$viteAzureClientId       = $secrets.ViteAzureClientId

if ([string]::IsNullOrWhiteSpace($neonConnStr))       { Write-Error "NeonConnectionString is not set in secrets.json." }
if ([string]::IsNullOrWhiteSpace($gcpProjectId))      { Write-Error "GcpProjectId is not set in secrets.json." }
if ([string]::IsNullOrWhiteSpace($gcpRegion))         { Write-Error "GcpRegion is not set in secrets.json." }
if ([string]::IsNullOrWhiteSpace($firebaseProjectId)) { Write-Error "FirebaseProjectId is not set in secrets.json." }
if ([string]::IsNullOrWhiteSpace($frontendUrl))       { Write-Error "FrontendUrl is not set in secrets.json." }
if ([string]::IsNullOrWhiteSpace($viteApiUrl))        { Write-Error "ViteApiUrl is not set in secrets.json." }
if ([string]::IsNullOrWhiteSpace($viteFirebaseApiKey)) { Write-Error "ViteFirebaseApiKey is not set in secrets.json." }

$imageTag        = "$gcpRegion-docker.pkg.dev/$gcpProjectId/apotheca/api:latest"
$cloudRunService = "apotheca-api"
$saName          = "apotheca-api"
$saEmail         = "$saName@$gcpProjectId.iam.gserviceaccount.com"
$dbSecretName    = "apotheca-db-connection-string"

# ---------------------------------------------------------------------------
# Read current frontend version (needed for the prompt below)
# ---------------------------------------------------------------------------
$frontendPath = Join-Path $sourcePath "web-frontend"
$envLocalFile = Join-Path $frontendPath ".env.local"
$currentVersion = "0.1.0"
if (Test-Path $envLocalFile) {
    $versionLine = Get-Content $envLocalFile | Where-Object { $_ -match "^VITE_APP_VERSION=" }
    if ($versionLine) { $currentVersion = $versionLine -replace "^VITE_APP_VERSION=", "" }
}

# ---------------------------------------------------------------------------
# Ask all deployment questions up front
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "==> Deployment options" -ForegroundColor Cyan
$runMigrations  = Read-Host "    Run database migrations?            (Y/N)"
$deployApi      = Read-Host "    Build and deploy API to Cloud Run?  (Y/N)"
$deployFrontend = Read-Host "    Deploy frontend to Firebase Hosting? (Y/N)"
if ($deployFrontend -eq 'Y' -or $deployFrontend -eq 'y') {
    $versionInput = Read-Host "    App version (current: $currentVersion - press Enter to keep)"
}
Write-Host ""

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
    "run.googleapis.com",
    "pubsub.googleapis.com"
)

foreach ($api in $requiredApis) {
    Invoke-GCloud services enable $api --project=$gcpProjectId | Out-Null
    Write-Host "    Enabled: $api" -ForegroundColor Green
}

# ---------------------------------------------------------------------------
# Phase 1: Run database migrations against Neon
# ---------------------------------------------------------------------------
Write-Host ""
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

$secretExists = Test-GCloudResource { "secrets", "describe", $dbSecretName, "--project=$gcpProjectId" }

if (-not $secretExists) {
    Write-Host "    Creating secret '$dbSecretName'..." -ForegroundColor Yellow
    Invoke-GCloud secrets create $dbSecretName `
        --project=$gcpProjectId `
        --replication-policy="automatic"
    $currentSecretValue = $null
} else {
    $currentSecretValue = & $gcloud secrets versions access latest --secret=$dbSecretName --project=$gcpProjectId 2>$null
}

if ($currentSecretValue -eq $neonConnStr) {
    Write-Host "    Secret '$dbSecretName' is already up to date, skipping." -ForegroundColor Gray
} else {
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
}

# ---------------------------------------------------------------------------
# Phases 3 + 4: Build and deploy API
# ---------------------------------------------------------------------------
Write-Host ""
if ($deployApi -eq 'Y' -or $deployApi -eq 'y') {

    # Phase 3: Build and push API image via Cloud Build
    Write-Host "    Building and pushing API image..." -ForegroundColor Cyan
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
        if ($LASTEXITCODE -ne 0) { Write-Error "Cloud Build failed." }
    } finally {
        Pop-Location
    }

    Write-Host "    Image built and pushed successfully." -ForegroundColor Green

    # Phase 4: Deploy to Cloud Run
    Write-Host "    Deploying to Cloud Run..." -ForegroundColor Cyan

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

    # Grant Firebase Authentication Admin so the service account can call GetUserAsync
    Invoke-GCloud projects add-iam-policy-binding $gcpProjectId `
        --member="serviceAccount:$saEmail" `
        --role="roles/firebaseauth.admin" | Out-Null
    Write-Host "    Granted Firebase Authentication Admin to service account." -ForegroundColor Green

    # Deploy the Cloud Run service
    Invoke-GCloud run deploy $cloudRunService `
        --image=$imageTag `
        --region=$gcpRegion `
        --platform=managed `
        --service-account=$saEmail `
        --set-secrets="ConnectionStrings__Postgres=$dbSecretName`:latest" `
        --set-env-vars="Firebase__ProjectId=$firebaseProjectId,Cors__AllowedOrigins__0=$frontendUrl,PubSub__RequireAuthentication=true,PubSub__Audience=$viteApiUrl" `
        --allow-unauthenticated `
        --project=$gcpProjectId

    if ($LASTEXITCODE -ne 0) { Write-Error "Cloud Run deployment failed." }

    $serviceUrl = & $gcloud run services describe $cloudRunService `
        --region=$gcpRegion `
        --project=$gcpProjectId `
        --format="value(status.url)" 2>$null

    Write-Host "    API deployed successfully." -ForegroundColor Green
    Write-Host "    Service URL: $serviceUrl" -ForegroundColor Green

    # ---------------------------------------------------------------------------
    # Pub/Sub: topics, subscriptions, and IAM
    # ---------------------------------------------------------------------------
    Write-Host ""
    Write-Host "==> Configuring Pub/Sub..." -ForegroundColor Cyan

    # Grant the API service account permission to publish
    Invoke-GCloud projects add-iam-policy-binding $gcpProjectId `
        --member="serviceAccount:$saEmail" `
        --role="roles/pubsub.publisher" | Out-Null
    Write-Host "    Granted pubsub.publisher to $saEmail" -ForegroundColor Green

    # Get the project number (needed for the Pub/Sub service agent email)
    $projectNumber = & $gcloud projects describe $gcpProjectId --format="value(projectNumber)" 2>$null
    $pubsubServiceAgent = "service-$projectNumber@gcp-sa-pubsub.iam.gserviceaccount.com"

    # Allow the Pub/Sub service agent to generate OIDC tokens for the API service account
    Invoke-GCloud iam service-accounts add-iam-policy-binding $saEmail `
        --member="serviceAccount:$pubsubServiceAgent" `
        --role="roles/iam.serviceAccountTokenCreator" `
        --project=$gcpProjectId | Out-Null
    Write-Host "    Granted serviceAccountTokenCreator to Pub/Sub service agent" -ForegroundColor Green

    # Create topics and push subscriptions
    $pubsubTopics = @(
        @{ TopicId = "note-deleted";  Path = "/events/notes/note-deleted" },
        @{ TopicId = "note-restored"; Path = "/events/notes/note-restored" }
    )

    foreach ($entry in $pubsubTopics) {
        $topicId   = $entry.TopicId
        $topicName = "projects/$gcpProjectId/topics/$topicId"
        $subName   = "projects/$gcpProjectId/subscriptions/$topicId-push"
        $pushUrl   = "$serviceUrl$($entry.Path)"

        if (-not (Test-GCloudResource { "pubsub", "topics", "describe", $topicName, "--project=$gcpProjectId" })) {
            Write-Host "    Creating topic '$topicId'..." -ForegroundColor Yellow
            Invoke-GCloud pubsub topics create $topicName --project=$gcpProjectId
        } else {
            Write-Host "    Topic '$topicId' already exists." -ForegroundColor Gray
        }

        if (-not (Test-GCloudResource { "pubsub", "subscriptions", "describe", $subName, "--project=$gcpProjectId" })) {
            Write-Host "    Creating subscription '$topicId-push'..." -ForegroundColor Yellow
            Invoke-GCloud pubsub subscriptions create $subName `
                --topic=$topicName `
                --push-endpoint=$pushUrl `
                --push-auth-service-account=$saEmail `
                --push-auth-token-audience=$serviceUrl `
                --ack-deadline=60 `
                --project=$gcpProjectId
        } else {
            Write-Host "    Updating subscription '$topicId-push' push endpoint..." -ForegroundColor Yellow
            Invoke-GCloud pubsub subscriptions modify-push-config $subName `
                --push-endpoint=$pushUrl `
                --push-auth-service-account=$saEmail `
                --push-auth-token-audience=$serviceUrl `
                --project=$gcpProjectId
        }
        Write-Host "    Subscription '$topicId-push' -> $pushUrl" -ForegroundColor Green
    }

} else {
    Write-Host "    Skipping API deployment." -ForegroundColor Gray
}


# ---------------------------------------------------------------------------
# Phase 5: Build and deploy frontend to Firebase Hosting
# ---------------------------------------------------------------------------
Write-Host ""
if ($deployFrontend -eq 'Y' -or $deployFrontend -eq 'y') {

    # Locate firebase CLI
    $firebase = Get-Command firebase -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source
    if (-not $firebase) {
        Write-Error "firebase CLI not found. Install with: npm install -g firebase-tools, then run 'firebase login'."
    }

    $envProdFile = Join-Path $frontendPath ".env.production"

    # Apply version change if one was entered
    if (-not [string]::IsNullOrWhiteSpace($versionInput)) {
        $currentVersion = $versionInput.Trim()
        if (Test-Path $envLocalFile) {
            $envLocalContent = Get-Content $envLocalFile -Raw
            if ($envLocalContent -match "(?m)^VITE_APP_VERSION=.*$") {
                $envLocalContent = $envLocalContent -replace "(?m)^VITE_APP_VERSION=.*$", "VITE_APP_VERSION=$currentVersion"
            } else {
                $envLocalContent = $envLocalContent.TrimEnd() + "`nVITE_APP_VERSION=$currentVersion`n"
            }
            [System.IO.File]::WriteAllText($envLocalFile, $envLocalContent, [System.Text.UTF8Encoding]::new($false))
        }
        Write-Host "    Version updated to: $currentVersion" -ForegroundColor Green
    } else {
        Write-Host "    Version unchanged:  $currentVersion" -ForegroundColor Gray
    }

    # Write production env file for the Vite build
    Write-Host "    Writing .env.production..." -ForegroundColor Cyan
    $envContent = @"
VITE_API_URL=$viteApiUrl
VITE_FIREBASE_API_KEY=$viteFirebaseApiKey
VITE_FIREBASE_AUTH_DOMAIN=$viteFirebaseAuthDomain
VITE_FIREBASE_PROJECT_ID=$viteFirebaseProjectId
VITE_FIREBASE_STORAGE_BUCKET=$viteFirebaseStorageBucket
VITE_FIREBASE_MESSAGING_SENDER_ID=$viteFirebaseMessagingSenderId
VITE_FIREBASE_APP_ID=$viteFirebaseAppId
VITE_AZURE_CLIENT_ID=$viteAzureClientId
VITE_APP_VERSION=$currentVersion
"@
    try {
        [System.IO.File]::WriteAllText($envProdFile, $envContent, [System.Text.UTF8Encoding]::new($false))

        # Build the frontend
        Write-Host "    Building frontend..." -ForegroundColor Cyan
        Push-Location $frontendPath
        try {
            npm run build
            if ($LASTEXITCODE -ne 0) { Write-Error "Frontend build failed." }
        } finally {
            Pop-Location
        }

        # Deploy to Firebase Hosting
        Write-Host "    Deploying to Firebase Hosting..." -ForegroundColor Cyan
        Push-Location $rootPath
        try {
            & $firebase deploy --only hosting --project=$firebaseProjectId
            if ($LASTEXITCODE -ne 0) { Write-Error "Firebase deploy failed." }
        } finally {
            Pop-Location
        }

        Write-Host "    Frontend deployed successfully." -ForegroundColor Green
    } finally {
        Remove-Item $envProdFile -ErrorAction SilentlyContinue
    }
} else {
    Write-Host "    Skipping frontend deployment." -ForegroundColor Gray
}

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "==> Deployment complete." -ForegroundColor Green
Write-Host ""
