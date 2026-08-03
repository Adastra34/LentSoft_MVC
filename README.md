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

## Manejo de Secretos y Configuración en Producción

### Desarrollo Local (User Secrets)
Para evitar subir credenciales sensibles al control de versiones (`git`), el proyecto utiliza **Secret Manager (.NET User Secrets)** en entorno local.

Los valores reales se gestionan de forma independiente por cada desarrollador:
```bash
cd LentSoft.Web
dotnet user-secrets set "PasswordResetJwt:SecretKey" "<CLAVE_CRIPTOGRAFICA_32_CARACTERES>"
dotnet user-secrets set "EmailSettings:SmtpUser" "tu-correo@gmail.com"
dotnet user-secrets set "EmailSettings:SmtpPassword" "tu-app-password"
```

### Producción (Variables de Entorno)
En servidores de producción, los valores confidenciales **NO deben incluirse en `appsettings.Production.json`**. Deben establecerse directamente como **Variables de Entorno** del sistema/servidor (usando doble guion bajo `__` para indicar la jerarquía en ASP.NET Core):

- `PasswordResetJwt__SecretKey` : Clave secreta aleatoria criptográfica (mínimo 32 caracteres).
- `EmailSettings__SmtpUser` : Usuario/Correo para el servidor SMTP.
- `EmailSettings__SmtpPassword` : Contraseña o token de aplicación SMTP.