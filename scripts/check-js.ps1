# scripts/check-js.ps1
# Validación de sintaxis JavaScript para el módulo AR de LentSoft

$ErrorActionPreference = "Stop"

# Asegurar que node esté disponible en PATH
if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    $machinePath = [System.Environment]::GetEnvironmentVariable("Path", "Machine")
    $userPath = [System.Environment]::GetEnvironmentVariable("Path", "User")
    $env:Path = "$machinePath;$userPath;$env:Path"
}

if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Error "Node.js no está disponible en PATH. Por favor instala Node.js para ejecutar esta verificación."
    exit 1
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir

$muestraMonturaPath = Join-Path $repoRoot "LentSoft.Web\wwwroot\js\muestra-montura.js"
$faceTrackingPath = Join-Path $repoRoot "LentSoft.Web\wwwroot\js\face-tracking.js"

if (-not (Test-Path $muestraMonturaPath)) {
    Write-Error "No se encontró el archivo: $muestraMonturaPath"
    exit 1
}

if (-not (Test-Path $faceTrackingPath)) {
    Write-Error "No se encontró el archivo: $faceTrackingPath"
    exit 1
}

Write-Host "Verificando sintaxis de archivos JavaScript..." -ForegroundColor Cyan

# 1. Comprobar muestra-montura.js
Write-Host "Verificando muestra-montura.js..."
& node --check $muestraMonturaPath
if ($LASTEXITCODE -ne 0) {
    Write-Error "Error de sintaxis en muestra-montura.js"
    exit $LASTEXITCODE
}
Write-Host "  [OK] muestra-montura.js válido." -ForegroundColor Green

# 2. Comprobar face-tracking.js (Módulo ES mediante entrada estándar)
Write-Host "Verificando face-tracking.js (ES Module)..."
Get-Content $faceTrackingPath -Raw | & node --check --input-type=module
if ($LASTEXITCODE -ne 0) {
    Write-Error "Error de sintaxis en face-tracking.js"
    exit $LASTEXITCODE
}
Write-Host "  [OK] face-tracking.js válido." -ForegroundColor Green

Write-Host "Todos los scripts de previsualización AR son válidos." -ForegroundColor Green
exit 0
