# Pruebas de Rendimiento - Gestion de Usuarios y Administracion

Este directorio contiene las pruebas de carga reproducibles implementadas con k6 para evaluar el desempeno del modulo de Gestion de Usuarios y Administracion en LentSoft_MVC (.NET 9, EF Core).

---

## 1. Objetivo

Evaluar la eficiencia, latencia y estabilidad del sistema bajo concurrencia sobre los dos flujos criticos del modulo:
1. **api_auth:** Autenticacion REST (`POST /api/auth/login`) y consulta de datos del usuario autenticado (`GET /api/users/me`), sometido a una rampa de 5 a 20 usuarios virtuales concurrentes.
2. **admin_listados:** Sesion de administracion web y consulta recurrente de listados con paginacion y filtros (`Dashboard/Admin` para clientes y trabajadores con `trabajadoresPageSize=20`), con 5 usuarios virtuales constantes.

---

## 2. Requisitos

- **Docker Desktop** (o Docker Engine en Linux) con soporte para Docker Compose.
- Opcional: **.NET SDK 9.0** si se prefiere ejecutar la aplicacion de forma local con `dotnet run`.

---

## 3. Preparacion del Entorno

### 3.1 Levantar la aplicacion con Docker Compose

Desde la raiz del repositorio:

```bash
docker compose up --build
```

La aplicacion estara disponible en `http://localhost:5000` (o `http://host.docker.internal:5000` desde el contenedor de k6).

### 3.2 Credenciales de prueba

Las credenciales corresponden a los usuarios inicializados por el sembrador de datos (`DbSeeder.cs`), documentadas en el README principal del proyecto:

- **Administrador:**
  - Email: `admin@lentsoft.com`
  - Contrasena: `admin123`
- **Usuario Cliente:**
  - Email: `user@lentsoft.com`
  - Contrasena: `user123`

---

## 4. Ejecucion de la Prueba de Carga con k6

No se requiere instalar k6 en el equipo anfitrion; se ejecuta directamente mediante la imagen oficial de Docker `grafana/k6`.

### 4.1 Ejecucion contra entorno Docker Compose

Desde la raiz del proyecto en PowerShell / Terminal:

```bash
docker run --rm -i -v "${PWD}/performance:/scripts" \
  -e BASE_URL=http://host.docker.internal:5000 \
  -e ADMIN_EMAIL=admin@lentsoft.com \
  -e ADMIN_PASSWORD=admin123 \
  -e USER_EMAIL=user@lentsoft.com \
  -e USER_PASSWORD=user123 \
  grafana/k6 run /scripts/usuarios-load-test.js
```

*Nota para Windows PowerShell:* Si la variable `${PWD}` no resuelve la ruta correctamente, se puede emplear `${pwd}` o la ruta absoluta local.

### 4.2 Ejecucion contra aplicacion local (`dotnet run`)

Si la aplicacion se ejecuta localmente mediante `dotnet run` escuchando en HTTPS con el certificado de desarrollo de ASP.NET Core, agregar la bandera `--insecure-skip-tls-verify` y definir la URL base HTTPS correspondiente:

```bash
docker run --rm -i -v "${PWD}/performance:/scripts" \
  -e BASE_URL=https://host.docker.internal:7245 \
  -e ADMIN_EMAIL=admin@lentsoft.com \
  -e ADMIN_PASSWORD=admin123 \
  -e USER_EMAIL=user@lentsoft.com \
  -e USER_PASSWORD=user123 \
  grafana/k6 run --insecure-skip-tls-verify /scripts/usuarios-load-test.js
```

---

## 5. Instrucciones para Comparacion "Antes vs Despues"

Para obtener una medicion comparativa estricta y reproducible del impacto de las optimizaciones:

1. **Medir el "Antes" (Commit base 47ba234):**
   ```bash
   git checkout 47ba234
   docker compose up --build -d
   # Ejecutar el comando de k6 indicado en la seccion 4
   # Registrar las metricas arrojadas en la columna 'Antes' de la plantilla
   docker compose down
   ```

2. **Volver a la rama de mejoras y medir el "Despues":**
   ```bash
   git checkout mejoras/eficiencia-usuarios-admin
   docker compose up --build -d
   # Ejecutar el mismo comando de k6
   # Registrar las metricas arrojadas en la columna 'Despues' de la plantilla
   docker compose down
   ```

---

## 6. Plantilla de Resultados

> Nota: Las celdas se presentan vacias para ser completadas tras la ejecucion real de las pruebas por parte del operador del entorno.

| Metrica | Antes (commit 47ba234) | Despues (esta rama) | Variacion (%) |
|---|---|---|---|
| p50 api_auth (ms) | | | |
| p95 api_auth (ms) | | | |
| Maximo api_auth (ms) | | | |
| p50 admin_listados (ms) | | | |
| p95 admin_listados (ms) | | | |
| Maximo admin_listados (ms) | | | |
| Tasa de errores (http_req_failed) | | | |
| Peticiones por segundo (req/s) | | | |

- **Fecha de medicion:** 
- **Especificaciones de la maquina:** 
- **Version de Docker:** 
