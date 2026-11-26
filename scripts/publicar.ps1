<#
.SYNOPSIS
    Compila y publica la aplicación LentSoft.Web en modo Release.
.DESCRIPTION
    Ejecuta 'dotnet publish -c Release -o C:\LentSoft\publish' para el proyecto LentSoft.Web.
#>

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$repoRoot = Split-Path -Parent $scriptDir
$projectPath = Join-Path $repoRoot "LentSoft.Web\LentSoft.Web.csproj"
$outputDir = "C:\LentSoft\publish"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " Publicando LentSoft.Web en Release" -ForegroundColor Cyan
Write-Host " Proyecto: $projectPath" -ForegroundColor Gray
Write-Host " Destino:  $outputDir" -ForegroundColor Gray
Write-Host "==========================================" -ForegroundColor Cyan

if (-not (Test-Path $outputDir)) {
    Write-Host "Creando directorio de destino: $outputDir" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

dotnet publish "$projectPath" -c Release -o "$outputDir"

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nPublicación completada exitosamente en: $outputDir" -ForegroundColor Green
} else {
    Write-Error "Error durante la publicación de LentSoft.Web. Código de salida: $LASTEXITCODE"
    exit $LASTEXITCODE
}
