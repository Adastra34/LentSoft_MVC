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
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<HistorialClinico> HistorialesClinicos => Set<HistorialClinico>();
    public DbSet<ExamenVisual> ExamenesVisuales => Set<ExamenVisual>();
    public DbSet<FormulaOptica> FormulasOpticas => Set<FormulaOptica>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Suppliers ──
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasIndex(e => e.Nombre);
            entity.HasIndex(e => e.Activo);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("GETUTCDATE()");
        });

        // ── InventoryMovements ──
        modelBuilder.Entity<InventoryMovement>(entity =>
        {
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.Fecha).IsDescending();
            entity.Property(e => e.Fecha).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.Product)
                  .WithMany()
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Users ──
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.NumeroDocumento).IsUnique();
            entity.HasIndex(e => e.Role);
            entity.Property(e => e.Role).HasDefaultValue("usuario");
            entity.Property(e => e.TipoDocumento).HasDefaultValue("CC");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("GETUTCDATE()");
            entity.ToTable(t => t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('usuario', 'admin', 'optometra', 'ventas')"));
        });

        // ── Products ──
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(e => e.Categoria);
            entity.HasIndex(e => e.Nombre);
            entity.HasIndex(e => e.Activo);
            entity.HasIndex(e => e.EsDestacado);
            entity.Property(e => e.Stock).HasDefaultValue(0);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.PorcentajeIva).HasDefaultValue(19.00m);
            entity.Property(e => e.Rating).HasDefaultValue(4.8m);
            entity.Property(e => e.ReviewCount).HasDefaultValue(12);
            entity.Property(e => e.EsDestacado).HasDefaultValue(false);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("GETUTCDATE()");
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

        // ── Carts ──
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.User)
                  .WithOne()
                  .HasForeignKey<Cart>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── CartItems ──
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasIndex(e => e.CartId);
            entity.HasIndex(e => e.ProductId);

            entity.HasOne(e => e.Cart)
                  .WithMany(c => c.CartItems)
                  .HasForeignKey(e => e.CartId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                  .WithMany()
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

        // ── HistorialesClinicos ──
        modelBuilder.Entity<HistorialClinico>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.OptometraId);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Optometra)
                  .WithMany()
                  .HasForeignKey(e => e.OptometraId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // ── ExamenesVisuales ──
        modelBuilder.Entity<ExamenVisual>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.OptometraId);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Optometra)
                  .WithMany()
                  .HasForeignKey(e => e.OptometraId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // ── FormulasOpticas ──
        modelBuilder.Entity<FormulaOptica>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.OptometraId);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Optometra)
                  .WithMany()
                  .HasForeignKey(e => e.OptometraId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // ── Seed Data (from database_schema.sql) ──
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

        // Products
        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = 1,
                Nombre = "Lentes Ray-Ban Aviator",
                Descripcion = "Lentes de sol clásicos estilo aviador",
                Precio = 2500000.00m,
                PorcentajeIva = 19.00m,
                Categoria = "lentes-sol",
                Marca = "Ray-Ban",
                Stock = 50,
                ImagenUrl = "https://images.unsplash.com/photo-1572635196237-14b3f281503f",
                Activo = true,
                Rating = 4.9m,
                ReviewCount = 28,
                EsDestacado = true,
                FechaCreacion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Material = "Metal",
                Color = "Negro / Verde G-15",
                Proteccion = "UV400",
                Estilo = "Aviador",
                Tamanio = "58-14-135",
                ImagenOverlayUrl = "/img/overlays/rayban_aviator.svg"
            },
            new Product
            {
                Id = 2,
                Nombre = "Lentes de Contacto Acuvue",
                Descripcion = "Lentes de contacto mensuales",
                Precio = 450000.00m,
                PrecioDescuento = 399000.00m,
                PorcentajeIva = 19.00m,
                Categoria = "lentes-contacto",
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
                Precio = 1800000.00m,
                PorcentajeIva = 19.00m,
                Categoria = "monturas",
                Marca = "Oakley",
                Stock = 30,
                Activo = true,
                Rating = 4.8m,
                ReviewCount = 15,
                EsDestacado = true,
                FechaCreacion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Material = "O-Matter (Plástico)",
                Color = "Negro Mate",
                Proteccion = "Filtro UV",
                Estilo = "Deportivo",
                Tamanio = "55-18-140",
                ImagenOverlayUrl = "/img/overlays/oakley_sport.svg"
            },
            new Product
            {
                Id = 4,
                Nombre = "Lentes Graduados Classic",
                Descripcion = "Lentes graduados con diseño clásico",
                Precio = 1200000.00m,
                PorcentajeIva = 19.00m,
                Categoria = "lentes-graduados",
                Marca = "LentSoft",
                Stock = 40,
                Activo = true,
                Rating = 4.6m,
                ReviewCount = 19,
                EsDestacado = true,
                FechaCreacion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Material = "Acetato",
                Color = "Carey",
                Proteccion = "Antirreflejo / Luz Azul",
                Estilo = "Wayfarer",
                Tamanio = "52-19-145",
                ImagenOverlayUrl = "/img/overlays/classic.svg"
            },
            new Product
            {
                Id = 5,
                Nombre = "Estuche Premium",
                Descripcion = "Estuche rígido para lentes",
                Precio = 150000.00m,
                PrecioDescuento = 99000.00m,
                PorcentajeIva = 19.00m,
                Categoria = "accesorios",
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
                Precio = 120000.00m,
                PorcentajeIva = 5.00m,
                Categoria = "accesorios",
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
                Salario = 2500000.00m,
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
                Salario = 1800000.00m,
                FechaContratacion = new DateTime(2025, 8, 15, 0, 0, 0, DateTimeKind.Utc),
                Activo = true
            },
            new Employee
            {
                Id = 3,
                Nombre = "Carlos Mendoza",
                Email = "carlos.mendoza@lentsoft.com",
                Telefono = "555-0103",
                Puesto = "Gerente",
                Departamento = "Administración",
                Salario = 3500000.00m,
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
                Total = 2500000.00m,
                Estado = "entregado",
                DireccionEnvio = "Calle 123 #45-67",
                FechaPedido = new DateTime(2026, 5, 15, 10, 0, 0, DateTimeKind.Utc)
            },
            new Order
            {
                Id = 2,
                UserId = 2,
                Total = 1800000.00m,
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
                PrecioUnitario = 2500000.00m
            },
            new OrderItem
            {
                Id = 2,
                OrderId = 2,
                ProductId = 3,
                Cantidad = 1,
                PrecioUnitario = 1800000.00m
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

        // Seed Suppliers
        modelBuilder.Entity<Supplier>().HasData(
            new Supplier { Id = "PROV001", Nombre = "Óptica Global S.A.", TipoProductos = "Monturas", Telefono = "555-1001", Correo = "ventas@opticaglobal.com", Activo = true, FechaRegistro = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Supplier { Id = "PROV002", Nombre = "LensTech Colombia", TipoProductos = "Lentes de contacto", Telefono = "555-1002", Correo = "contacto@lenstech.co", Activo = true, FechaRegistro = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Supplier { Id = "PROV003", Nombre = "Distribuidora Visual", TipoProductos = "Accesorios", Telefono = "555-1003", Correo = "info@distvisual.com", Activo = true, FechaRegistro = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        // Seed Inventory Movements
        modelBuilder.Entity<InventoryMovement>().HasData(
            new InventoryMovement { Id = 1, ProductId = 1, Tipo = "Entrada", Cantidad = 20, Fecha = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc), Responsable = "Administrador" },
            new InventoryMovement { Id = 2, ProductId = 2, Tipo = "Salida", Cantidad = 5, Fecha = new DateTime(2026, 5, 2, 14, 0, 0, DateTimeKind.Utc), Responsable = "Administrador" },
            new InventoryMovement { Id = 3, ProductId = 3, Tipo = "Entrada", Cantidad = 10, Fecha = new DateTime(2026, 5, 3, 11, 30, 0, DateTimeKind.Utc), Responsable = "Administrador" }
        );
    }
}
