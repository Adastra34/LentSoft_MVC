# Módulo de Gestión de Usuarios y Administración - LentSoft

Este documento describe la arquitectura, reglas de negocio, flujos de autenticación, políticas de seguridad y endpoints de la API del módulo de **Gestión de Usuarios y Admin** en LentSoft_MVC (.NET 9 ASP.NET Core).

---

## 1. Flujos Principales

### 1.1 Flujo de Registro Web
1. El usuario accede a `/Auth/Register` e ingresa sus datos (Nombre, Apellido, Tipo de documento, Número de documento, Correo, Teléfono y Contraseña).
2. Se normaliza el correo con `Trim()` y `ToLowerInvariant()`.
3. Se verifica que ni el correo ni el número de documento existan previamente en la base de datos.
4. La contraseña se hashea de forma segura con **BCrypt**.
5. Se asigna obligatoriamente el rol `usuario` y el estado `Activo = true`.
6. Al completarse el registro, se crea automáticamente la sesión con cookie de autenticación y se redirige a `/Dashboard/Usuario`.

### 1.2 Flujo de Inicio de Sesión (Login) y Bloqueo Temporal
1. El usuario accede a `/Auth/Login` con su correo y contraseña.
2. Se genera una clave de cliente compuesta por: `{email.Trim().ToLowerInvariant()}:{clientIp}`.
3. Se consulta `ILoginAttemptService.IsLockedOut(key, out var remaining)`:
   - Si está bloqueado, se rechaza la petición indicando los minutos restantes: `"Demasiados intentos fallidos. Intenta de nuevo en X minuto(s)."`.
4. `AuthService.LoginAsync` busca usuarios activos (`Activo == true`) con el correo normalizado y verifica la contraseña con `BCrypt.Verify`:
   - Si las credenciales fallan o el usuario está inactivo, se invoca `RegisterFailure(key)` y se devuelve el mensaje genérico: `"Credenciales inválidas. Intenta nuevamente."` (sin revelar si la cuenta existe o si está inactiva).
   - Si se alcanza el 5.º intento fallido dentro de la ventana de 15 minutos, se activa el bloqueo temporal por 15 minutos y se registra una advertencia en `ILogger`.
5. Si el login es exitoso, se ejecuta `Reset(key)` para reiniciar el contador de intentos, se emite la cookie (`LentSoft.Auth`) con vigencia de 24 horas y se redirige al panel según el rol del usuario.

### 1.3 Flujo de Recuperación de Contraseña (Token de un solo uso)
1. El usuario solicita la recuperación en `/Auth/ForgotPassword` o un administrador crea un cliente sin contraseña en `/User/CreateClient`.
2. `IPasswordResetTokenService.GenerateToken(userId, email, passwordHash)` genera un token JWT con vigencia de 30 minutos que contiene:
   - Claim `userId`
   - Claim `email`
   - Claim `purpose = "password_reset"`
   - Claim `pwdfp`: Primeros 16 caracteres hexadecimales del `SHA256(currentPasswordHash)`.
3. Se envía el correo con el enlace firmado al usuario mediante `IEmailService.SendPasswordResetEmailAsync`.
4. Al ingresar al enlace (`/Auth/ResetPassword?token=...`), el sistema valida:
   - Firma, emisor, audiencia y vigencia del JWT.
   - Propósito `password_reset`.
   - `MatchesCurrentPassword(token, user.PasswordHash)`: Se recalcula el fingerprint del hash actual del usuario en la base de datos y se compara contra el claim `pwdfp` del token.
5. Si el usuario ya cambió su contraseña previamente, el fingerprint en base de datos difiere del token, marcándolo inmediatamente como **inválido**. De esta forma, el token es de **un solo uso** sin requerir tablas adicionales en la base de datos.

---

## 2. Roles del Sistema y Redirecciones

Al iniciar sesión exitosamente en la aplicación web, el controlador redirige al usuario a su panel correspondiente según su rol:

| Rol | Redirección por Defecto | Controlador / Acción | Descripción |
|---|---|---|---|
| `admin` | `/Dashboard/Admin` | `DashboardController.Admin` | Panel integral de administración, gestión de usuarios, clientes, trabajadores, inventario y métricas. |
| `optometra` | `/Optometra/Index` | `OptometraController.Index` | Módulo de gestión de citas, historias clínicas y prescripciones ópticas. |
| `ventas` | `/Ventas/Index` | `VentasController.Index` | Módulo comercial de caja, facturas y pedidos. |
| `usuario` | `/Dashboard/Usuario` | `DashboardController.Usuario` | Portal de clientes: compras, citas personales, fórmulas y favoritos. |

---

## 3. Reglas de Negocio de Administración

* **Protección del Último Administrador Activo (RF-M07-03):** El sistema no permite eliminar o desactivar al único usuario con rol `admin` que se encuentre activo (`Activo == true`). Los administradores inactivos no se contabilizan para esta regla.
* **Prohibición de Auto-Desactivación:** Un administrador autenticado no puede desactivar su propia cuenta (`id == currentUserId`).
* **Manejo Seguro de Excepciones:** Ningún endpoint o acción de administración expone excepciones técnicas (`ex.Message`) al usuario final. Ante errores imprevistos, se registra el detalle en `ILogger` y se presenta un mensaje genérico amigable: `"No se pudo completar la operación. Intenta de nuevo."`.
* **Creación de Clientes por Administrador:**
  - Si el administrador **no** escribe contraseña, el sistema autogenera una credencial aleatoria segura, crea al cliente y envía un enlace de definición de contraseña por correo. En pantalla no se exhiben contraseñas en texto claro.
  - Si el administrador **sí** define una contraseña, esta debe cumplir rigurosamente con la política de contraseñas seguras.
* **Normalización de Identificadores:** Todos los correos electrónicos se normalizan aplicando `Trim()` y `ToLowerInvariant()` para evitar colisiones y duplicados por diferencias de mayúsculas/minúsculas o espacios accidentales.

---

## 4. Política de Contraseñas

Las contraseñas ingresadas tanto en el registro de usuarios como en el restablecimiento y en la creación manual por administradores deben cumplir la siguiente política:
* Longitud mínima de **8 caracteres**.
* Al menos una letra **minúscula** (`[a-z]`).
* Al menos una letra **mayúscula** (`[A-Z]`).
* Al menos un **número** (`[0-9]`).
* Al menos un **carácter especial** (`[^a-zA-Z0-9]`).

Expresión regular aplicada:
```regex
^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$
```

---

## 5. Política de Bloqueo Temporal

* **Umbral de fallos:** 5 intentos fallidos consecutivos.
* **Ventana móvil:** 15 minutos.
* **Duración del bloqueo:** 15 minutos a partir del quinto intento fallido.
* **Composición de la clave:** Correo normalizado en minúsculas + dirección IP del cliente (`{email}:{ip}`). Esto previene que un atacante externo bloquee a un usuario legítimo intentando contraseñas desde una IP distinta.
* **Reseteo del contador:** Cada inicio de sesión exitoso reinicia el contador de intentos.

---

## 6. Endpoints de la API REST

### 6.1 Autenticación Móvil y API (`/api/auth`)
* `POST /api/auth/login`
  - **Body:** `{ "email": "string", "password": "string" }`
  - **Respuesta 200:** Retorna `{ token, user }` con el JWT firmado para la aplicación móvil.
  - **Respuesta 401:** Credenciales inválidas o usuario inactivo.
  - **Respuesta 429:** Umbral de intentos fallidos excedido (`{ message: "Demasiados intentos fallidos. Intenta de nuevo en X minuto(s)." }`).
* `POST /api/auth/register`
  - **Body:** `{ "nombre", "apellido", "tipoDocumento", "numeroDocumento", "email", "telefono", "password" }`
  - **Respuesta 200:** `{ token, user }`.
  - **Respuesta 400:** Correo o documento ya registrados.
* `POST /api/auth/forgot-password`
  - **Body:** `{ "email": "string" }`
  - **Respuesta 200:** `{ success: true, message: "..." }`.

### 6.2 Perfil de Usuario (`/api/users`)
* `GET /api/users/me`
  - Requiere encabezado `Authorization: Bearer <token>`.
  - Retorna información del perfil del usuario autenticado.
* `PUT /api/users/me`
  - Requiere encabezado `Authorization: Bearer <token>`.
  - Permite actualizar nombre, apellido, teléfono y dirección.

---

## 7. Ejecución de Pruebas Automatizadas

El proyecto cuenta con una suite completa de pruebas unitarias basadas en **xUnit** y SQLite en memoria en el proyecto `LentSoft.Tests`.

Para compilar la solución y ejecutar todas las pruebas:

```bash
dotnet test LentSoft.Tests
```

Para ejecutar pruebas filtrando por una clase específica:
```bash
dotnet test LentSoft.Tests --filter FullyQualifiedName~AuthServiceTests
dotnet test LentSoft.Tests --filter FullyQualifiedName~LoginAttemptServiceTests
dotnet test LentSoft.Tests --filter FullyQualifiedName~PasswordResetTokenServiceTests
dotnet test LentSoft.Tests --filter FullyQualifiedName~UserServiceTests
```

---

## 8. Rendimiento

### 8.1 Optimizaciones de Desempeño Implementadas

1. **Eliminación de consultas N+1 en listado de trabajadores (`DashboardController.Admin`):**
   - Anteriormente, para cada empleado de la página visible se ejecutaba una consulta `FirstOrDefaultAsync` sobre la tabla `Users` (por correo) y otra `CountAsync` sobre `Orders` (por `UserId` y `Activo`), generando hasta 2 * N consultas adicionales por recarga.
   - Se reemplazó por un esquema de consultas por lotes (batching):
     - Una consulta para obtener identificadores y correos de los usuarios correspondientes a la lista de correos de empleados de la página mediante `AsNoTracking()`.
     - Una consulta agregada (`GroupBy` y `Count`) sobre `Orders` filtrando únicamente por los identificadores de usuarios obtenidos y con `AsNoTracking()`.
     - Resolución y mapeo en memoria mediante diccionarios con comparación de correos insensible a mayúsculas/minúsculas (`StringComparer.OrdinalIgnoreCase`) de forma equivalente a la colación por defecto de la base de datos (sin invocar `ToLower()` en la consulta SQL).
     - El conteo de pedidos activos (`PedidosCount`) devuelve exactamente el mismo valor que antes (0 si el empleado no tiene usuario asociado).
     - El número de consultas extra pasó de ser dependiente del tamaño de página a un máximo constante de 2 consultas adicionales en total.

2. **Consultas de solo lectura con `AsNoTracking()`:**
   - Se aplicó `AsNoTracking()` a `clientesQuery` y `trabajadoresQuery` en `DashboardController.Admin`, así como a las consultas por lotes de usuarios y pedidos. Se verificó previamente que las entidades no sufren modificaciones posteriores en el método, eliminando la sobrecarga de seguimiento de cambios en EF Core.

3. **Límites estrictos en parámetros de paginación:**
   - Se limitaron los parámetros `clientesPage` y `trabajadoresPage` a un mínimo de 1 (`Math.Max(1, page)`).
   - Se acotaron los tamaños de página `clientesPageSize` y `trabajadoresPageSize` a un rango seguro entre 1 y 100 mediante `Math.Clamp`, manteniendo los valores por defecto (5) y previniendo problemas de sobrecarga por valores arbitrarios o negativos.

4. **Compresión de respuestas HTTP (Brotli y Gzip):**
   - Se configuró `builder.Services.AddResponseCompression` en `Program.cs` con compresión Brotli y Gzip en nivel `CompressionLevel.Fastest` y `EnableForHttps = true`.
   - Se restringieron los tipos MIME exclusivamente a recursos estáticos y JSON: `text/css`, `application/javascript`, `text/javascript`, `application/json`, `image/svg+xml`.
   - El middleware `UseResponseCompression` se integró inmediatamente antes de `UseStaticFiles`.

### 8.2 Decisiones de Arquitectura: Aspectos NO modificados y Justificación

1. **Índice compuesto en base de datos (`Role` + `Activo`):**
   - Requiere la creación de una nueva migración de EF Core y alteración del esquema de la base de datos. Para mantener un alcance quirúrgico y evitar conflictos con otros módulos en desarrollo concurrente, no se realizaron modificaciones al esquema ni a `LentSoftDbContext`.

2. **Exclusión de `text/html` de la compresión HTTP:**
   - Las páginas HTML contienen tokens antiforgery dinámicos y datos confidenciales. Comprimir respuestas dinámicas sobre HTTPS expondría la aplicación a ataques de canal lateral tipo **BREACH** (Browser Reconnaissance and Exfiltration via Adaptive Compression of Hypertext).

3. **Costo computacional del login con BCrypt:**
   - La verificación de contraseñas con BCrypt consume ciclos de CPU de manera intencional (factor de costo de trabajo) para brindar robustez frente a ataques de fuerza bruta y ataques por diccionario fuera de línea. Esta decisión de seguridad no se compromete en favor de la latencia.

### 8.3 Ejecución de Pruebas de Carga

Las pruebas de carga reproducibles se encuentran implementadas en la carpeta `performance/` mediante k6.

Para ejecutarlas sin necesidad de instalar k6 localmente:

```bash
docker run --rm -i -v "${PWD}/performance:/scripts" \
  -e BASE_URL=http://host.docker.internal:5000 \
  -e ADMIN_EMAIL=admin@lentsoft.com \
  -e ADMIN_PASSWORD=admin123 \
  -e USER_EMAIL=user@lentsoft.com \
  -e USER_PASSWORD=user123 \
  grafana/k6 run /scripts/usuarios-load-test.js
```

Para consultar la descripción detallada de los escenarios (`api_auth` y `admin_listados`), los umbrales configurados y la plantilla de resultados "Antes vs Después", revisar `performance/README.md`.
