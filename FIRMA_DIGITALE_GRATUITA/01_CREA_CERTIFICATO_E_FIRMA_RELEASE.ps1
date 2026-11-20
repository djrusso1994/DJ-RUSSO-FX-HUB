#requires -Version 5.1
# DJ RUSSO - personal code-signing for locally built Windows executables.
# Private key is generated and stored ONLY in CurrentUser\My.
[CmdletBinding()]
param([string]$OutputDirectory = "")
$ErrorActionPreference = 'Stop'
$subject = 'CN=Giuseppe Russo - DJ RUSSO'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Split-Path -Parent $here
$cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert | Where-Object {
    $_.Subject -eq $subject -and $_.HasPrivateKey -and $_.NotAfter -gt (Get-Date).AddDays(7)
} | Sort-Object NotAfter -Descending | Select-Object -First 1
if (-not $cert) {
    Write-Host 'Creo il certificato personale gratuito in CurrentUser\\My...' -ForegroundColor Cyan
    $cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject $subject `
        -CertStoreLocation 'Cert:\CurrentUser\My' -KeyAlgorithm RSA -KeyLength 3072 `
        -HashAlgorithm SHA256 -KeyExportPolicy NonExportable -NotAfter (Get-Date).AddYears(3)
}
Write-Host ('Certificato: ' + $cert.Subject)
Write-Host ('Thumbprint: ' + $cert.Thumbprint)
Write-Host 'Nota: firma autofirmata, non riconosciuta automaticamente sugli altri PC.' -ForegroundColor Yellow
$release = if ($OutputDirectory) { $OutputDirectory } else { Join-Path $project 'bin\Release' }
$hubCandidates = @()
if (Test-Path $release) {
    $hubCandidates = @(Get-ChildItem -LiteralPath $release -Filter 'DJRussoFXHub.exe' -File -Recurse | Sort-Object FullName)
}
if ($hubCandidates.Count -eq 0) {
    Write-Warning 'DJRussoFXHub.exe non trovato in bin\\Release. Compila Release/x64 prima di firmare.'
}
$targets = @()
$targets += $hubCandidates
# Sign ONLY the copies emitted by the build, not the original payload in the source tree.
if (Test-Path $release) {
    $targets += @(Get-ChildItem -LiteralPath $release -Filter 'DJ RUSSO METER X.exe' -File -Recurse)
}
$targets = @($targets | Sort-Object FullName -Unique)
if ($targets.Count -eq 0) { throw 'Nessun EXE Release trovato. Compila prima il progetto in Release / x64.' }
foreach ($file in $targets) {
    Write-Host ('Firma: ' + $file.FullName) -ForegroundColor Cyan
    $signed = Set-AuthenticodeSignature -FilePath $file.FullName -Certificate $cert -HashAlgorithm SHA256
    $check = Get-AuthenticodeSignature -FilePath $file.FullName
    if (-not $check.SignerCertificate -or $check.SignerCertificate.Thumbprint -ne $cert.Thumbprint) {
        throw ('La firma non risulta presente su: ' + $file.FullName + ' - ' + $signed.StatusMessage)
    }
    Write-Host ('Firma presente. Stato Windows: ' + $check.Status + ' - ' + $check.StatusMessage) -ForegroundColor Green
}
Write-Host 'Firmati gli EXE Release trovati. Non e stata modificata la copia sorgente del payload.' -ForegroundColor Green
Write-Host 'ATTENZIONE: su PC non attendibili gli avvisi Autore sconosciuto / SmartScreen possono rimanere.' -ForegroundColor Yellow
# Non-interactive: runs automatically after Release build.
