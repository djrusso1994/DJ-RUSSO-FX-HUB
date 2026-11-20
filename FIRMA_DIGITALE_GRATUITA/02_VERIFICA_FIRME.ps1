#requires -Version 5.1
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$release = Join-Path $root 'bin\Release'
if (-not (Test-Path $release)) { Write-Host 'Compila Release/x64 prima.'; Read-Host 'INVIO'; exit 1 }
$files = @(Get-ChildItem -LiteralPath $release -Recurse -File | Where-Object { $_.Name -in @('DJRussoFXHub.exe','DJ RUSSO METER X.exe') })
if ($files.Count -eq 0) { Write-Host 'Nessun EXE trovato.'; Read-Host 'INVIO'; exit 1 }
foreach ($file in $files) {
    $sig=Get-AuthenticodeSignature -FilePath $file.FullName
    Write-Host ('File: ' + $file.FullName)
    Write-Host ('Stato: ' + $sig.Status + ' - ' + $sig.StatusMessage)
    if ($sig.SignerCertificate) { Write-Host ('Firmatario: ' + $sig.SignerCertificate.Subject); Write-Host ('Thumbprint: ' + $sig.SignerCertificate.Thumbprint) }
}
Read-Host 'Premi INVIO per chiudere'
