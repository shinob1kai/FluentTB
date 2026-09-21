#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Creates a self-signed certificate and signs FluentTB.exe
.DESCRIPTION
    This prevents Windows Defender false positives with Costura.Fody
#>

$ErrorActionPreference = "Stop"

Write-Host "=== FluentTB Code Signing ===" -ForegroundColor Cyan
Write-Host ""

$certName = "FluentTB Code Signing"
$exePath = "bin\Release\FluentTB.exe"

# Check if EXE exists
if (-not (Test-Path $exePath)) {
    Write-Host "ERROR: FluentTB.exe not found at $exePath" -ForegroundColor Red
    Write-Host "Please build the project first:" -ForegroundColor Yellow
    Write-Host "  dotnet build FluentTB.csproj -c Release" -ForegroundColor Gray
    exit 1
}

# Check if certificate already exists
$existingCert = Get-ChildItem -Path Cert:\CurrentUser\My | Where-Object { $_.Subject -like "*$certName*" } | Select-Object -First 1

if ($existingCert) {
    Write-Host "[1/3] Using existing certificate..." -ForegroundColor Green
    $cert = $existingCert
    Write-Host "  Thumbprint: $($cert.Thumbprint)" -ForegroundColor Gray
} else {
    Write-Host "[1/3] Creating self-signed certificate..." -ForegroundColor Green
    $cert = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject "CN=$certName" `
        -KeyUsage DigitalSignature `
        -FriendlyName $certName `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3") `
        -NotAfter (Get-Date).AddYears(5)
    
    Write-Host "  Created certificate with thumbprint: $($cert.Thumbprint)" -ForegroundColor Gray
    
    # Export to trusted root
    Write-Host "[2/3] Installing certificate to Trusted Root..." -ForegroundColor Green
    $certPath = "Cert:\CurrentUser\My\$($cert.Thumbprint)"
    Export-Certificate -Cert $certPath -FilePath "$env:TEMP\fluenttb-cert.cer" | Out-Null
    Import-Certificate -FilePath "$env:TEMP\fluenttb-cert.cer" -CertStoreLocation Cert:\CurrentUser\Root | Out-Null
    Remove-Item "$env:TEMP\fluenttb-cert.cer" -Force
    Write-Host "  Certificate installed" -ForegroundColor Gray
}

# Sign the executable
Write-Host "[3/3] Signing FluentTB.exe..." -ForegroundColor Green
Set-AuthenticodeSignature -FilePath $exePath -Certificate $cert -TimestampServer "http://timestamp.digicert.com" | Out-Null

# Verify signature
$signature = Get-AuthenticodeSignature -FilePath $exePath
if ($signature.Status -eq "Valid") {
    Write-Host "  Successfully signed!" -ForegroundColor Green
    Write-Host ""
    Write-Host "=== Signing Complete ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "Signature Details:" -ForegroundColor Cyan
    Write-Host "  Status: $($signature.Status)" -ForegroundColor White
    Write-Host "  Signer: $($signature.SignerCertificate.Subject)" -ForegroundColor White
    Write-Host "  Timestamp: $($signature.TimeStamperCertificate.NotBefore)" -ForegroundColor White
    Write-Host ""
    Write-Host "Windows Defender should no longer flag this file." -ForegroundColor Green
} else {
    Write-Host "  WARNING: Signature status: $($signature.Status)" -ForegroundColor Yellow
    Write-Host "  StatusMessage: $($signature.StatusMessage)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Note: For production releases, use a proper code signing certificate" -ForegroundColor Gray
Write-Host "from a trusted Certificate Authority (CA)." -ForegroundColor Gray
