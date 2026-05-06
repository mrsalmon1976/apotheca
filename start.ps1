# Start CPDAId development environment

$root = $PSScriptRoot

# 1. Start Docker services
Write-Host "Starting Docker services..." -ForegroundColor Cyan
docker compose -f "$root\docker-compose.yml" up -d

# 2. Open a new console window for the frontend dev server
Write-Host "Starting frontend dev server..." -ForegroundColor Cyan
$frontendCmd = "Set-Location '$root\source\web-frontend'; npm run dev"
$frontendEncoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($frontendCmd))
wt -w 0 new-tab -- powershell -NoExit -EncodedCommand $frontendEncoded

# 3. Start Claude Code
Write-Host "Starting Claude Code..." -ForegroundColor Cyan
Set-Location $root
claude
