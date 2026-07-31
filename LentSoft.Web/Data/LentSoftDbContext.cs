using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Data;

public class LentSoftDbContext : DbContext
{
    public LentSoftDbContext(DbContextOptions<LentSoftDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Users ──
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.NumeroDocumento).IsUnique();
            entity.HasIndex(e => e.Role);
            entity.Property(e => e.Role).HasDefaultValue("usuario");
            entity.Property(e => e.TipoDocumento).HasDefaultValue("CC");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("GETUTCDATE()");
            entity.HasCheckConstraint("CK_Users_Role", "[Role] IN ('usuario', 'admin', 'optometra', 'ventas')");
        });

        // ── Categorias ──
        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasIndex(e => e.Nombre).IsUnique();
        });

        // ── Products ──
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(e => e.CategoriaId);
            entity.HasIndex(e => e.Nombre);
            entity.HasIndex(e => e.Activo);
            entity.HasIndex(e => e.EsDestacado);
            entity.Property(e => e.Stock).HasDefaultValue(0);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Rating).HasDefaultValue(4.8m);
            entity.Property(e => e.ReviewCount).HasDefaultValue(12);
            entity.Property(e => e.EsDestacado).HasDefaultValue(false);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.Categoria)
                  .WithMany(c => c.Products)
                  .HasForeignKey(e => e.CategoriaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Favorites ──
        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
            entity.Property(e => e.FechaAgregado).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Favorites)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                  .WithMany(p => p.Favorites)
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Orders ──
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Estado);
            entity.HasIndex(e => e.FechaPedido).IsDescending();
            entity.Property(e => e.Estado).HasDefaultValue("pendiente");
            entity.Property(e => e.FechaPedido).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Orders)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // ── OrderItems ──
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.ProductId);

            entity.HasOne(e => e.Order)
                  .WithMany(o => o.OrderItems)
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                  .WithMany(p => p.OrderItems)
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // ── Employees ──
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Departamento);
            entity.HasIndex(e => e.Activo);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaContratacion).HasDefaultValueSql("GETUTCDATE()");
        });

        // ── Invoices ──
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasIndex(e => e.NumeroFactura).IsUnique();
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.Estado);
            entity.Property(e => e.Estado).HasDefaultValue("pendiente");
            entity.Property(e => e.FechaEmision).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.Order)
                  .WithMany(o => o.Invoices)
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Appointments ──
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.FechaHora);
            entity.HasIndex(e => e.Estado);
            entity.Property(e => e.Estado).HasDefaultValue("pendiente");
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Appointments)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Proveedores ──
        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.HasIndex(e => e.Nombre);
            entity.Property(e => e.Estado).HasDefaultValue("activo");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("GETUTCDATE()");
        });

        // ── MovimientosInventario ──
        modelBuilder.Entity<MovimientoInventario>(entity =>
        {
            entity.HasIndex(e => e.Fecha).IsDescending();
            entity.HasIndex(e => e.Tipo);
            entity.Property(e => e.Fecha).HasDefaultValueSql("GETUTCDATE()");
        });

        // ── Seed Data ──
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Users
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Nombre = "Administrador",
                Apellido = "Sistema",
                TipoDocumento = "CC",
                NumeroDocumento = "1000000001",
                Email = "admin@lentsoft.com",
                PasswordHash = "$2a$11$MJPUqK7jAM6tEvUkExo1cO/3cmh4MpxnXNVPg./4kKzlsqAwPW/oq", // admin123
                Role = "admin",
                FechaRegistro = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id = 2,
                Nombre = "Usuario",
                Apellido = "Demo",
                TipoDocumento = "CC",
                NumeroDocumento = "1000000002",
                Email = "user@lentsoft.com",
                PasswordHash = "$2a$11$q43GcbtmtTn9FyysOC73SO4HUFfBAF43GzPuZ6y0d0EZeDitCKqGa", // user123
                Role = "usuario",
                FechaRegistro = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id = 3,
                Nombre = "María",
                Apellido = "García",
                TipoDocumento = "CC",
                NumeroDocumento = "1000000003",
                Email = "optometra@lentsoft.com",
                PasswordHash = "$2a$11$MJPUqK7jAM6tEvUkExo1cO/3cmh4MpxnXNVPg./4kKzlsqAwPW/oq", // admin123
                Role = "optometra",
                FechaRegistro = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id = 4,
                Nombre = "Juan",
                Apellido = "Pérez",
                TipoDocumento = "CC",
                NumeroDocumento = "1000000004",
                Email = "ventas@lentsoft.com",
                PasswordHash = "$2a$11$MJPUqK7jAM6tEvUkExo1cO/3cmh4MpxnXNVPg./4kKzlsqAwPW/oq", // admin123
                Role = "ventas",
                FechaRegistro = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Categorias (1=Gafas, 2=Lentes, 3=Accesorios)
        modelBuilder.Entity<Categoria>().HasData(
            new Categoria { Id = 1, Nombre = "Gafas" },
            new Categoria { Id = 2, Nombre = "Lentes" },
            new Categoria { Id = 3, Nombre = "Accesorios" }
        );

        // Products (con CategoriaId en lugar de Categoria string)
        // Gafas (1): lentes de sol, monturas
        // Lentes (2): lentes de contacto, lentes graduados
        // Accesorios (3): estuche, líquido limpiador
        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = 1,
                Nombre = "Lentes Ray-Ban Aviator",
                Descripcion = "Lentes de sol clásicos estilo aviador",
                Precio = 2500.00m,
                CategoriaId = 1, // Gafas
                Marca = "Ray-Ban",
                Stock = 50,
                ImagenUrl = "https://images.unsplash.com/photo-1572635196237-14b3f281503f",
                Activo = true,
                Rating = 4.9m,
                ReviewCount = 28,
                EsDestacado = true,
                FechaCreacion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Product
            {
                Id = 2,
                Nombre = "Lentes de Contacto Acuvue",
                Descripcion = "Lentes de contacto mensuales",
                Precio = 450.00m,
                PrecioDescuento = 399.00m,
                CategoriaId = 2, // Lentes
                Marca = "Acuvue",
                Stock = 100,
                Activo = true,
                Rating = 4.7m,
                ReviewCount = 42,
                EsDestacado = true,
                FechaCreacion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Product
            {
                Id = 3,
                Nombre = "Montura Oakley Sport",
                Descripcion = "Montura deportiva ultraligera",
                Precio = 1800.00m,
                CategoriaId = 1, // Gafas
                Marca = "Oakley",
                Stock = 30,
                Activo = true,
                Rating = 4.8m,
                ReviewCount = 15,
                EsDestacado = true,
                FechaCreacion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Product
            {
                Id = 4,
                Nombre = "Lentes Graduados Classic",
                Descripcion = "Lentes graduados con diseño clásico",
                Precio = 1200.00m,
                CategoriaId = 2, // Lentes
                Marca = "LentSoft",
                Stock = 40,
                Activo = true,
                Rating = 4.6m,
                ReviewCount = 19,
                EsDestacado = true,
                FechaCreacion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Product
            {
                Id = 5,
                Nombre = "Estuche Premium",
                Descripcion = "Estuche rígido para lentes",
                Precio = 150.00m,
                PrecioDescuento = 99.00m,
                CategoriaId = 3, // Accesorios
                Marca = "LentSoft",
                Stock = 200,
                Activo = true,
                Rating = 4.5m,
                ReviewCount = 8,
                EsDestacado = false,
                FechaCreacion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Product
            {
                Id = 6,
                Nombre = "Líquido Limpiador",
                Descripcion = "Solución limpiadora para lentes 360ml",
                Precio = 120.00m,
                CategoriaId = 3, // Accesorios
                Marca = "Opti-Free",
                Stock = 150,
                Activo = true,
                Rating = 4.9m,
                ReviewCount = 33,
                EsDestacado = false,
                FechaCreacion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed Sample Favorites
        modelBuilder.Entity<Favorite>().HasData(
            new Favorite { Id = 1, UserId = 2, ProductId = 1, FechaAgregado = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Favorite { Id = 2, UserId = 2, ProductId = 2, FechaAgregado = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        // Employees
        modelBuilder.Entity<Employee>().HasData(
            new Employee
            {
                Id = 1,
                Nombre = "María García",
                Email = "maria.garcia@lentsoft.com",
                Telefono = "555-0101",
                Puesto = "Optometrista",
                Departamento = "Atención al Cliente",
                Salario = 25000.00m,
                FechaContratacion = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                Activo = true
            },
            new Employee
            {
                Id = 2,
                Nombre = "Juan Pérez",
                Email = "juan.perez@lentsoft.com",
                Telefono = "555-0102",
                Puesto = "Vendedor",
                Departamento = "Ventas",
                Salario = 18000.00m,
                FechaContratacion = new DateTime(2025, 8, 15, 0, 0, 0, DateTimeKind.Utc),
                Activo = true
            },
            new Employee
            {
                Id = 3,
                Nombre = "Ana Martínez",
                Email = "ana.martinez@lentsoft.com",
                Telefono = "555-0103",
                Puesto = "Gerente",
                Departamento = "Administración",
                Salario = 35000.00m,
                FechaContratacion = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                Activo = true
            }
        );

        // Sample orders
        modelBuilder.Entity<Order>().HasData(
            new Order
            {
                Id = 1,
                UserId = 2,
                Total = 2500.00m,
                Estado = "entregado",
                DireccionEnvio = "Calle 123 #45-67",
                FechaPedido = new DateTime(2026, 5, 15, 10, 0, 0, DateTimeKind.Utc)
            },
            new Order
            {
                Id = 2,
                UserId = 2,
                Total = 1800.00m,
                Estado = "enviado",
                DireccionEnvio = "Calle 123 #45-67",
                FechaPedido = new DateTime(2026, 5, 20, 14, 30, 0, DateTimeKind.Utc)
            }
        );

        // Sample order items
        modelBuilder.Entity<OrderItem>().HasData(
            new OrderItem
            {
                Id = 1,
                OrderId = 1,
                ProductId = 1,
                Cantidad = 1,
                PrecioUnitario = 2500.00m
            },
            new OrderItem
            {
                Id = 2,
                OrderId = 2,
                ProductId = 3,
                Cantidad = 1,
                PrecioUnitario = 1800.00m
            }
        );

        // Sample appointments
        modelBuilder.Entity<Appointment>().HasData(
            new Appointment
            {
                Id = 1,
                UserId = 2,
                Servicio = "Examen de vista",
                FechaHora = new DateTime(2026, 5, 25, 10, 0, 0, DateTimeKind.Utc),
                Estado = "confirmada",
                FechaCreacion = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc)
            },
            new Appointment
            {
                Id = 2,
                UserId = 2,
                Servicio = "Ajuste de lentes",
                FechaHora = new DateTime(2026, 6, 2, 14, 30, 0, DateTimeKind.Utc),
                Estado = "pendiente",
                FechaCreacion = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed Proveedores (migrados desde los mock)
        modelBuilder.Entity<Proveedor>().HasData(
            new Proveedor { Id = 1, Nombre = "Óptica Global S.A.", Contacto = "Carlos Ruiz", Telefono = "555-1001", Email = "ventas@opticaglobal.com", TipoProducto = "Monturas", Estado = "activo", FechaRegistro = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Proveedor { Id = 2, Nombre = "LensTech Colombia", Contacto = "Ana López", Telefono = "555-1002", Email = "contacto@lenstech.co", TipoProducto = "Lentes de contacto", Estado = "activo", FechaRegistro = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Proveedor { Id = 3, Nombre = "Distribuidora Visual", Contacto = "Pedro Gómez", Telefono = "555-1003", Email = "info@distvisual.com", TipoProducto = "Accesorios", Estado = "activo", FechaRegistro = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Proveedor { Id = 4, Nombre = "Ray-Ban Distribuidor", Contacto = "María Fernández", Telefono = "555-1004", Email = "dist@rayban.co", TipoProducto = "Lentes de sol", Estado = "activo", FechaRegistro = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Proveedor { Id = 5, Nombre = "Oakley Partner", Contacto = "José Martínez", Telefono = "555-1005", Email = "partner@oakley.co", TipoProducto = "Monturas deportivas", Estado = "inactivo", FechaRegistro = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        // Seed MovimientosInventario (migrados desde los mock)
        modelBuilder.Entity<MovimientoInventario>().HasData(
            new MovimientoInventario { Id = 1, Producto = "Lentes Ray-Ban Aviator", Tipo = "entrada", Cantidad = 20, Fecha = new DateTime(2026, 7, 25, 0, 0, 0, DateTimeKind.Utc), Responsable = "Ana Martínez" },
            new MovimientoInventario { Id = 2, Producto = "Lentes de Contacto Acuvue", Tipo = "salida", Cantidad = 5, Fecha = new DateTime(2026, 7, 26, 0, 0, 0, DateTimeKind.Utc), Responsable = "Juan Pérez" },
            new MovimientoInventario { Id = 3, Producto = "Montura Oakley Sport", Tipo = "entrada", Cantidad = 10, Fecha = new DateTime(2026, 7, 24, 0, 0, 0, DateTimeKind.Utc), Responsable = "Ana Martínez" },
            new MovimientoInventario { Id = 4, Producto = "Estuche Premium", Tipo = "salida", Cantidad = 15, Fecha = new DateTime(2026, 7, 26, 0, 0, 0, DateTimeKind.Utc), Responsable = "Juan Pérez" },
            new MovimientoInventario { Id = 5, Producto = "Líquido Limpiador", Tipo = "entrada", Cantidad = 50, Fecha = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc), Responsable = "Ana Martínez" }
        );
    }
}
