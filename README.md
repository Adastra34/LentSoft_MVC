# LentSoft - Plataforma E-commerce Óptico

## Descripción

LentSoft es una plataforma de comercio electrónico especializada en productos ópticos. Originalmente desarrollada con arquitecturas mixtas (React y Vanilla JS), ha sido migrada a una arquitectura unificada y robusta utilizando **ASP.NET Core MVC 9.0**.

## Tecnologías Utilizadas

- **Framework:** ASP.NET Core MVC 9.0
- **Base de Datos:** SQL Server LocalDB
- **ORM:** Entity Framework Core
- **Autenticación:** ASP.NET Core Identity (Basado en cookies) y BCrypt
- **Frontend:** Razor Views (.cshtml), HTML5, CSS3, JS Minimalista

## Estructura del Proyecto

El proyecto sigue el patrón MVC estándar de ASP.NET Core:

```text
LentSoft.Web/
├── Controllers/       # Controladores MVC (Home, Auth, Product, Order, Dashboard)
├── Models/            # Entidades de base de datos y ViewModels
│   ├── Entities/      # Clases mapeadas a base de datos (User, Product, Order, etc.)
│   └── ViewModels/    # Clases para transferencia de datos a vistas
├── Services/          # Lógica de negocio (Interfaces y clases)
├── Data/              # Configuración de Entity Framework (DbContext y Seed Data)
├── Views/             # Vistas Razor estructuradas por controlador
└── wwwroot/           # Archivos estáticos (CSS, JS, Imágenes)
```

## Configuración y Ejecución

### Requisitos Previos

- [.NET SDK 9.0 o superior](https://dotnet.microsoft.com/download)
- **SQL Server LocalDB** (incluido de forma estándar en Visual Studio, o instalable por separado).
- Herramienta de Entity Framework Core CLI (se puede instalar globalmente ejecutando: `dotnet tool install --global dotnet-ef`).

### Pasos para ejecutar localmente desde cero

1. **Iniciar la instancia de base de datos local (LocalDB)**:
   Asegúrate de que la instancia `MSSQLLocalDB` esté iniciada en tu equipo ejecutando en la terminal:
   ```bash
   sqllocaldb start MSSQLLocalDB
   ```
   *(Si por alguna razón la instancia no existiera, la puedes crear primero con `sqllocaldb create MSSQLLocalDB`).*

2. **Navega a la carpeta del proyecto web**:
   ```bash
   cd LentSoft.Web
   ```

3. **Restaurar y aplicar las migraciones a la base de datos**:
   Ejecuta el siguiente comando para crear la base de datos `LentSoftDB_Dev` y aplicar todo el historial de migraciones desde cero:
   ```bash
   dotnet ef database update
   ```

4. **Compilar y ejecutar la aplicación**:
   ```bash
   dotnet run
   ```

Al iniciar, el sistema también ejecutará automáticamente el sembrador de datos (`DbSeeder.cs`) para registrar los pacientes de prueba, citas, exámenes, fórmulas e historias clínicas sin duplicar datos.

## Usuarios de Prueba

Puedes probar los diferentes roles con las siguientes credenciales:

- **Administrador:**
  - Email: `admin@lentsoft.com`
  - Contraseña: `admin123`
- **Optómetra:**
  - Email: `optometra@lentsoft.com`
  - Contraseña: `admin123`
- **Ventas:**
  - Email: `ventas@lentsoft.com`
  - Contraseña: `admin123`
- **Usuario Cliente:**
  - Email: `user@lentsoft.com`
  - Contraseña: `user123`

## Manejo de Configuración Local y Secretos

### Cadena de Conexión (Portabilidad)
En `appsettings.json`, las cadenas de conexión no tienen valores fijos locales por seguridad y portabilidad. Puedes configurarla de dos formas recomendadas:

#### Opción A: Secretos de Usuario (.NET User Secrets - Recomendado para desarrollo local)
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\mssqllocaldb;Database=LentSoftDB_Dev;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True" --project LentSoft.Web
```

#### Opción B: Variables de Entorno (Recomendado para servidores y CI/CD)
```bash
# PowerShell / Windows:
$env:ConnectionStrings__DefaultConnection = "Server=(localdb)\mssqllocaldb;Database=LentSoftDB_Dev;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"

# Linux / macOS / Bash:
export ConnectionStrings__DefaultConnection="Server=tcp:tudbserver,1433;Database=LentSoftDB;User Id=usr;Password=pass;TrustServerCertificate=True;"
```

*Nota:* Si no se especifica una cadena de conexión a SQL Server o se habilita `"UseSqlite": true`, el sistema alternará automáticamente a SQLite (`LentSoft.db`), ideal para desarrollo rápido y entornos portátiles.

### Desarrollo Local (`appsettings.Development.json`)
Para evitar subir credenciales sensibles al control de versiones (`git`), el proyecto utiliza un archivo de configuración local `appsettings.Development.json` (el cual está excluido en `.gitignore`).

Para correr el proyecto localmente:
1. Copia `LentSoft.Web/appsettings.Development.example.json` a `LentSoft.Web/appsettings.Development.json`.
2. Completa tus propios valores de prueba (puede ser una cuenta Gmail descartable con contraseña de aplicación para el envío de correos).

### Ejecución de Pruebas Unitarias
Para ejecutar la suite de pruebas unitarias automatizadas (xUnit):
```bash
dotnet test
```

### Producción y Despliegue en Otro Servidor

En entornos de producción o al desplegar en otro servidor (IIS, Linux con Kestrel/Nginx, Docker, Azure App Service), las migraciones automáticas y seeds están deshabilitadas al inicio por seguridad e integridad de datos (`app.Environment.IsDevelopment()`).

#### Requisitos en el Servidor
- **ASP.NET Core Runtime 9.0** (o .NET SDK 9.0 si se compila en el servidor).
- **SQL Server 2019 o superior**, Azure SQL o SQL Server en Docker/Linux.
- Certificado SSL/TLS válido para HTTPS (requerido por los navegadores para WebRTC y acceso a la cámara).

#### 1. Configuración de Variables de Entorno
Configura las siguientes variables en el servidor o contenedor (usando doble guión bajo `__` para jerarquías de configuración en ASP.NET Core):

| Variable de Entorno | Descripción / Ejemplo |
| :--- | :--- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | `Server=tcp:tudbserver,1433;Database=LentSoftDB;User Id=usr;Password=pass;TrustServerCertificate=True;` |
| `PasswordResetJwt__SecretKey` | Clave criptográfica aleatoria (mínimo 32 caracteres) |
| `SaleConfirmationJwt__SecretKey` | Clave criptográfica aleatoria (mínimo 32 caracteres) |
| `MobileJwt__SecretKey` | Clave criptográfica para API Móvil (mínimo 32 caracteres) |
| `EmailSettings__SmtpServer` | Ej: `smtp.gmail.com` o servidor SMTP empresarial |
| `EmailSettings__SmtpPort` | Ej: `587` |
| `EmailSettings__SmtpUser` | Usuario / Correo de envío |
| `EmailSettings__SmtpPassword` | Contraseña o token de aplicación SMTP |
| `EmailSettings__FromEmail` | Correo remitente |
| `EmailSettings__FromName` | Nombre del remitente (`LentSoft Óptica`) |
| `Gemini__ApiKey` | API Key de Google Gemini para el asistente virtual Morgana |

*(Puedes guiarte con los ejemplos incluidos en `LentSoft.Web/appsettings.Example.json`).*

#### 2. Comandos de Migración de Base de Datos
Para aplicar las migraciones en el servidor de destino antes de iniciar el aplicativo:

```bash
# Aplicar todas las migraciones pendientes directamente a la base de datos configurada:
dotnet ef database update --project LentSoft.Web

# O generar un script SQL idempotente para el DBA:
dotnet ef migrations script --idempotent --project LentSoft.Web -o deploy_migrations.sql
```

#### 3. Publicación y Ejecución
```bash
# Generar artefacto publicado optimizado
dotnet publish LentSoft.Web/LentSoft.Web.csproj -c Release -o ./publish

# Iniciar la aplicación en el servidor
cd ./publish
dotnet LentSoft.Web.dll
```

### Módulo de Previsualización AR (Webcam & Face-Tracking)

El módulo de realidad aumentada permite a los clientes probarse virtualmente las monturas en tiempo real usando su cámara web o móvil.

#### Características Principales
- **Procesamiento 100% en el cliente:** El tracking facial opera enteramente en el navegador del usuario utilizando MediaPipe Tasks Vision (WebAssembly + Canvas HTML5).
- **Seguridad y Privacidad:** Las capturas y fotogramas de la cámara web **nunca** son transmitidos ni almacenados en servidores de LentSoft.
- **Rendimiento Adaptativo:** Lazy loading al presionar "Activar Cámara", detección de iluminación, suavizado de jitter (*smoothing*), tolerancia a pérdida temporal de rostros y limitación adaptativa de FPS para dispositivos de gama baja.
- **Alta Disponibilidad (Local con Fallback CDN):** Carga preferente de binarios locales (`/lib/mediapipe/`) y respaldo automático a CDNs públicos si falla la carga local. Soporte dual de aceleración por hardware (`GPU`) con tolerancia y degradación elegante a `CPU`.

#### Archivos que Componen el Módulo
| Archivo | Rol en el Sistema |
| :--- | :--- |
| `LentSoft.Web/Views/Product/MuestraMontura.cshtml` | Vista Razor que renderiza el catálogo responsive (2 columnas en escritorio, 1 en móvil) y el contenedor de vista previa interactiva. |
| `LentSoft.Web/wwwroot/js/muestra-montura.js` | Controlador UI de cámara, control de errores, selección de variantes, soporte dinámico de i18n mediante `getLang()` y estado de botones. |
| `LentSoft.Web/wwwroot/js/face-tracking.js` | Módulo ES que encapsula MediaPipe FaceLandmarker, ciclo de renderizado sobre canvas, ajuste de calibración y estimación de puntos faciales (iris 468 y 473). |
| `LentSoft.Web/wwwroot/lib/mediapipe/` | Paquete local autónomo: `vision_bundle.mjs`, `wasm/vision_wasm_internal.*` y `models/face_landmarker.task`. |
| `LentSoft.Web/Services/ProductService.cs` | Consulta `GetGafasAsync()` que filtra de forma insensible a mayúsculas monturas y lentes de sol excluyendo lentes de contacto y accesorios. |
| `LentSoft.Web/Data/DbSeeder.cs` y `LentSoftDbContext.cs` | Semillero de datos y calibración por defecto para overlays existentes. |
| `LentSoft.Web/Program.cs` | Configuración de `FileExtensionContentTypeProvider` para servir `.wasm` (`application/wasm`) y `.task` (`application/octet-stream`). |
| `LentSoft.Tests/ArPreviewContractTests.cs` | Suite de pruebas de contrato y guardas anti-regresión xUnit. |
| `scripts/check-js.ps1` | Script de validación de sintaxis para JavaScript (`node --check`). |

#### Qué NO se debe tocar sin correr `dotnet test`
1. **Rutas e integridad de MediaPipe local:** No mover ni borrar archivos en `wwwroot/lib/mediapipe/` sin actualizar los contratos en `ArPreviewContractTests.cs`.
2. **Scripts inline en `MuestraMontura.cshtml`:** No volver a insertar lógica de negocio o referencias sueltas a `lang` dentro de bloques `<script>` en la vista. Toda la lógica debe permanecer en `muestra-montura.js`.
3. **Manejo de idioma en `muestra-montura.js`:** Toda referencia al idioma activo debe resolverse invocando `getLang()` en tiempo de ejecución.
4. **Archivos de overlay:** Toda ruta de overlay referenciada en `DbSeeder.cs` o `LentSoftDbContext.cs` debe existir físicamente en `wwwroot/img/overlays/`.
5. **Filtro de catálogo:** Cualquier modificación en `GetGafasAsync()` debe preservar la exclusión de lentes de contacto y accesorios.

#### Cómo Agregar un Overlay Nuevo
1. **Crear el diseño del marco:** Diseñar o exportar el marco en formato **PNG con fondo transparente**, asegurando que los cristales/lentes sean transparentes y el marco esté centrado y nivelado horizontalmente.
2. **Copiar a wwwroot:** Guardar el archivo en la carpeta `LentSoft.Web/wwwroot/img/overlays/` (ejemplo: `mi_montura_optica.png`).
3. **Configurar desde el Panel de Administración o Base de Datos:**
   - **`ImagenOverlayUrl`**: Ruta relativa al archivo, ej: `/img/overlays/mi_montura_optica.png`.
   - **`EscalaOverlay`**: Factor de escala para ajustar el ancho de la montura respecto a la distancia entre pupilas (valor base recomendado: `2.30`).
   - **`OffsetXOverlay`**: Ajuste horizontal fino (`0.00` centrado).
   - **`OffsetYOverlay`**: Ajuste vertical fino (`0.12` para situar el puente sobre la nariz).
   *(Si un producto no tiene `ImagenOverlayUrl`, el catálogo mostrará la etiqueta "Sin prueba AR" y seguirá siendo visible y comprable sin causar errores).*