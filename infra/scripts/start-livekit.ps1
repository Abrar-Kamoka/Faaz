<#
.SYNOPSIS
    Starts a local LiveKit dev server for video/audio sessions. Without this running, joining a
    session fails at the room-token/room-creation step — bookings, reminders, and everything else
    work fine, only the actual video call can't be established.

.DESCRIPTION
    Dev-only. Production uses LiveKit Cloud (see appsettings.json, no server to run yourself).
    This starts the official livekit-server binary (https://github.com/livekit/livekit) in --dev
    mode against infra/tools/livekit/config.yaml, whose API key/secret MUST match
    Faaz.Services.Booking.WebHost/appsettings.Development.json's LiveKit:ApiKey / LiveKit:ApiSecret
    — LiveKit uses symmetric HMAC signing, so a mismatch fails token verification even though both
    sides are individually "configured."

    If the binary isn't present yet (it's gitignored — large, platform-specific, trivially
    re-fetched), this script downloads the official release for this platform from GitHub and
    verifies its SHA256 checksum against the published checksums.txt before running anything.

.PARAMETER Version
    LiveKit server release to install if missing. Keep in sync with config.yaml's comment if bumped.

.EXAMPLE
    ./start-livekit.ps1
#>
param(
    [string]$Version = "1.13.6"
)

$ErrorActionPreference = "Stop"
$toolDir   = Join-Path $PSScriptRoot "..\tools\livekit"
$exePath   = Join-Path $toolDir "livekit-server.exe"
$configPath = Join-Path $toolDir "config.yaml"

$existing = Get-Process -Name "livekit-server" -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "livekit-server is already running (PID $($existing.Id -join ', ')) - nothing to do." -ForegroundColor Yellow
    exit 0
}

if (-not (Test-Path $exePath)) {
    Write-Host "livekit-server.exe not found - downloading official v$Version release for Windows..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Force -Path $toolDir | Out-Null

    $zipUrl   = "https://github.com/livekit/livekit/releases/download/v$Version/livekit_${Version}_windows_amd64.zip"
    $sumsUrl  = "https://github.com/livekit/livekit/releases/download/v$Version/checksums.txt"
    $zipPath  = Join-Path $toolDir "livekit.zip"
    $sumsPath = Join-Path $toolDir "checksums.txt"

    Invoke-WebRequest -Uri $zipUrl -OutFile $zipPath
    Invoke-WebRequest -Uri $sumsUrl -OutFile $sumsPath

    $expected = (Select-String -Path $sumsPath -Pattern "windows_amd64\.zip$").Line.Split(' ')[0].Trim()
    $actual   = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash.ToLower()
    if ($actual -ne $expected) {
        Remove-Item $zipPath, $sumsPath -Force
        Write-Error "Checksum mismatch for downloaded livekit-server.exe (expected $expected, got $actual). Not running an unverified binary — aborting."
        exit 1
    }
    Write-Host "Checksum verified." -ForegroundColor Green

    Expand-Archive -Path $zipPath -DestinationPath $toolDir -Force
    Remove-Item $zipPath, $sumsPath -Force
}

if (-not (Test-Path $configPath)) {
    Write-Error "Missing $configPath - see infra/tools/livekit/config.yaml in git history, or run '$exePath generate-keys' and update both it and appsettings.Development.json together."
    exit 1
}

Write-Host "Starting LiveKit dev server on http://localhost:7880 ..." -ForegroundColor Cyan
Write-Host "Leave this window open while you test sessions. Ctrl+C to stop.`n" -ForegroundColor Cyan

& $exePath --dev --config $configPath
