<#
.SYNOPSIS
    Instala y configura el servicio de Windows para LentSoft.
.DESCRIPTION
    Crea el servicio Windows 'LentSoft' con sc.exe apuntando al ejecutable publicado,
    configura el inicio automático, la recuperación ante fallos (reinicio a los 5 s)
    y asigna la variable de entorno ASPNETCORE_ENVIRONMENT=Production exclusivamente
    para este servicio en el Registro de Windows.
    Debe ejecutarse en una consola de PowerShell como Administrador.
#>

[CmdletBinding()]
param(
    [string]$ServiceName = "LentSoft",
    [string]$PublishDir = "C:\LentSoft\publish",
    [string]$ExeName = "LentSoft.Web.exe"
)

# 1. Comprobar privilegios de Administrador
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Error "Este script requiere privilegios de Administrador. Ejecuta PowerShell como Administrador e inténtalo de nuevo."
    exit 1
}

$binPath = Join-Path $PublishDir $ExeName

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host " Instalador del Servicio de Windows: $ServiceName" -ForegroundColor Cyan
Write-Host " Ruta del binario: $binPath" -ForegroundColor Gray
Write-Host "=================================================" -ForegroundColor Cyan

if (-not (Test-Path $binPath)) {
    Write-Warning "El archivo '$binPath' no existe actualmente."
    Write-Warning "Recuerda ejecutar primero 'scripts\publicar.ps1' antes de iniciar el servicio."
}

# 2. Si el servicio ya existe, detenerlo y eliminarlo para reinstalación limpia
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "El servicio '$ServiceName' ya existe. Deteniendo y eliminando para reinstalar..." -ForegroundColor Yellow
    if ($existingService.Status -eq 'Running') {
        Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    }
    & sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

# 3. Crear el servicio con sc.exe (inicio automático)
# Nota: sc.exe requiere un espacio después de 'binPath=', 'start=' y 'DisplayName='
Write-Host "Creando el servicio '$ServiceName'..." -ForegroundColor Green
$createResult = & sc.exe create $ServiceName binPath= "`"$binPath`"" start= auto DisplayName= "LentSoft"
Write-Host $createResult

# 4. Configurar descripción del servicio
& sc.exe description $ServiceName "Servicio Web LentSoft MVC detras de Cloudflare Tunnel" | Out-Null

# 5. Configurar política de recuperación ante fallos: reiniciar a los 5 s (reset diario: 86400 s)
Write-Host "Configurando acciones de recuperación ante fallos (reinicio en 5s)..." -ForegroundColor Green
$failureResult = & sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/5000/restart/5000
Write-Host $failureResult

# 6. Definir ASPNETCORE_ENVIRONMENT=Production solo para este servicio en el Registro
Write-Host "Configurando ASPNETCORE_ENVIRONMENT=Production en el Registro del servicio..." -ForegroundColor Green
$serviceRegistryPath = "HKLM:\SYSTEM\CurrentControlSet\Services\$ServiceName"

if (-not (Test-Path $serviceRegistryPath)) {
    Write-Error "No se encontró la clave de registro del servicio en $serviceRegistryPath"
    exit 1
}

Set-ItemProperty -Path $serviceRegistryPath -Name "Environment" -Value @("ASPNETCORE_ENVIRONMENT=Production") -Type MultiString

Write-Host "`nConfiguración completada con éxito." -ForegroundColor Green
Write-Host "Para iniciar el servicio:" -ForegroundColor White
Write-Host "  Start-Service -Name $ServiceName" -ForegroundColor Yellow
Write-Host "O usando sc:" -ForegroundColor White
Write-Host "  sc.exe start $ServiceName" -ForegroundColor Yellow
