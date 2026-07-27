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
- SQL Server LocalDB (incluido en Visual Studio) o cualquier otra instancia de SQL Server.

### Pasos para ejecutar localmente

1. Navega a la carpeta del proyecto web:
   ```bash
   cd LentSoft.Web
   ```

2. (Opcional) Compila el proyecto para asegurar que no hay errores:
   ```bash
   dotnet build
   ```

3. Ejecuta la aplicación:
   ```bash
   dotnet run
   ```

Al iniciar por primera vez en entorno de Desarrollo (`Development`), la aplicación aplicará automáticamente las migraciones a la base de datos y sembrará los datos de prueba iniciales.

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