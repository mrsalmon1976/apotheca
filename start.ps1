# Start CPDAId development environment

$root = $PSScriptRoot

# 1. Ensure Docker is running
Write-Host "Checking if Docker is running..." -ForegroundColor Cyan
docker info >$null 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Docker is not running. Starting Docker Desktop..." -ForegroundColor Yellow
    & 'C:\Program Files\Docker\Docker\Docker Desktop.exe'
    
    # Wait for Docker to start (up to 2 minutes)
    $maxRetries = 24
    $retryCount = 0
    while ($retryCount -lt $maxRetries) {
        docker info >$null 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Docker is now running." -ForegroundColor Green
            break
        }
        Write-Host "Waiting for Docker daemon... ($($retryCount + 1)/$maxRetries)" -ForegroundColor Gray
        Start-Sleep -Seconds 5
        $retryCount++
    }

    if ($retryCount -eq $maxRetries) {
        Write-Host "Docker failed to start in a timely manner. Exiting." -ForegroundColor Red
        exit 1
    }
}

# 2. Start Docker services
Write-Host "Starting Docker services..." -ForegroundColor Cyan
docker compose -f "$root\docker-compose.yml" up -d
if ($LASTEXITCODE -ne 0) {
    Write-Host "Docker compose failed. Exiting." -ForegroundColor Red
    exit $LASTEXITCODE
}

# 3. Open a new console window for the frontend dev server
Write-Host "Starting frontend dev server..." -ForegroundColor Cyan
$frontendCmd = "Set-Location '$root\source\web-frontend'; npm run dev"
$frontendEncoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($frontendCmd))
wt -w 0 new-tab -- powershell -NoExit -EncodedCommand $frontendEncoded

# 3. Start Claude Code
Write-Host "Starting Claude Code..." -ForegroundColor Cyan
Set-Location $root
claude
