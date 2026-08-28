<#
.SYNOPSIS
    Starts the Stripe CLI webhook forwarder for local dev. Without this running, Stripe
    has no way to reach localhost, so no payment ever confirms - bookings sit in
    SlotReserved until the 10-minute hold expires and auto-cancel.

.DESCRIPTION
    Dev-only. Production doesn't need this at all — Stripe calls your server's public
    URL directly there via a Webhook Endpoint registered in the Stripe Dashboard, with
    its own signing secret. This script only solves "localhost has no public URL."

.PARAMETER ForwardTo
    Override the local Payment service webhook URL. Defaults to the dev launchSettings port.

.EXAMPLE
    ./start-stripe-listen.ps1
#>
param(
    [string]$ForwardTo = "http://localhost:55235/webhooks/stripe"
)

if (-not (Get-Command stripe -ErrorAction SilentlyContinue)) {
    Write-Error "Stripe CLI not found on PATH. Install it: https://stripe.com/docs/stripe-cli"
    exit 1
}

$existing = Get-Process -Name "stripe" -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "stripe listen is already running (PID $($existing.Id -join ', ')) - nothing to do." -ForegroundColor Yellow
    exit 0
}

Write-Host "Starting Stripe webhook forwarder -> $ForwardTo" -ForegroundColor Cyan
Write-Host "Leave this window open while you test bookings/payments. Ctrl+C to stop.`n" -ForegroundColor Cyan

stripe listen --forward-to $ForwardTo
