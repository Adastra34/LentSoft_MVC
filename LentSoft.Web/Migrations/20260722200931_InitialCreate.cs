using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LentSoft.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Puesto = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Departamento = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Salario = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    FechaContratacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Precio = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PrecioDescuento = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Categoria = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Marca = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Stock = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ImagenUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "usuario"),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UltimaCompra = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Servicio = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "pendiente"),
                    Notas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appointments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "pendiente"),
                    DireccionEnvio = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaPedido = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    FechaEntrega = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumeroFactura = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Impuestos = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "pendiente"),
                    FechaEmision = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MetodoPago = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "Id", "Activo", "Departamento", "Email", "FechaContratacion", "Nombre", "Puesto", "Salario", "Telefono" },
                values: new object[,]
                {
                    { 1, true, "Atención al Cliente", "maria.garcia@lentsoft.com", new DateTime(2025, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), "María García", "Optometrista", 25000.00m, "555-0101" },
                    { 2, true, "Ventas", "juan.perez@lentsoft.com", new DateTime(2025, 8, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Juan Pérez", "Vendedor", 18000.00m, "555-0102" },
                    { 3, true, "Administración", "ana.martinez@lentsoft.com", new DateTime(2024, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Ana Martínez", "Gerente", 35000.00m, "555-0103" }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Activo", "Categoria", "Descripcion", "FechaCreacion", "ImagenUrl", "Marca", "Nombre", "Precio", "PrecioDescuento", "Stock" },
                values: new object[,]
                {
                    { 1, true, "lentes-sol", "Lentes de sol clásicos estilo aviador", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "https://images.unsplash.com/photo-1572635196237-14b3f281503f", "Ray-Ban", "Lentes Ray-Ban Aviator", 2500.00m, null, 50 },
                    { 2, true, "lentes-contacto", "Lentes de contacto mensuales", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Acuvue", "Lentes de Contacto Acuvue", 450.00m, 399.00m, 100 },
                    { 3, true, "monturas", "Montura deportiva ultraligera", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Oakley", "Montura Oakley Sport", 1800.00m, null, 30 },
                    { 4, true, "lentes-graduados", "Lentes graduados con diseño clásico", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "LentSoft", "Lentes Graduados Classic", 1200.00m, null, 40 },
                    { 5, true, "accesorios", "Estuche rígido para lentes", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "LentSoft", "Estuche Premium", 150.00m, 99.00m, 200 },
                    { 6, true, "accesorios", "Solución limpiadora para lentes 360ml", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Opti-Free", "Líquido Limpiador", 120.00m, null, 150 }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "FechaRegistro", "Nombre", "PasswordHash", "Role", "Telefono", "UltimaCompra" },
                values: new object[,]
                {
                    { 1, "admin@lentsoft.com", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Administrador", "$2a$11$MJPUqK7jAM6tEvUkExo1cO/3cmh4MpxnXNVPg./4kKzlsqAwPW/oq", "admin", null, null },
                    { 2, "user@lentsoft.com", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Usuario Demo", "$2a$11$q43GcbtmtTn9FyysOC73SO4HUFfBAF43GzPuZ6y0d0EZeDitCKqGa", "usuario", null, null }
                });

            migrationBuilder.InsertData(
                table: "Appointments",
                columns: new[] { "Id", "Estado", "FechaCreacion", "FechaHora", "Notas", "Servicio", "UserId" },
                values: new object[,]
                {
                    { 1, "confirmada", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 25, 10, 0, 0, 0, DateTimeKind.Utc), null, "Examen de vista", 2 },
                    { 2, "pendiente", new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 2, 14, 30, 0, 0, DateTimeKind.Utc), null, "Ajuste de lentes", 2 }
                });

            migrationBuilder.InsertData(
                table: "Orders",
                columns: new[] { "Id", "DireccionEnvio", "Estado", "FechaEntrega", "FechaPedido", "Total", "UserId" },
                values: new object[,]
                {
                    { 1, "Calle 123 #45-67", "entregado", null, new DateTime(2026, 5, 15, 10, 0, 0, 0, DateTimeKind.Utc), 2500.00m, 2 },
                    { 2, "Calle 123 #45-67", "enviado", null, new DateTime(2026, 5, 20, 14, 30, 0, 0, DateTimeKind.Utc), 1800.00m, 2 }
                });

            migrationBuilder.InsertData(
                table: "OrderItems",
                columns: new[] { "Id", "Cantidad", "OrderId", "PrecioUnitario", "ProductId" },
                values: new object[,]
                {
                    { 1, 1, 1, 2500.00m, 1 },
                    { 2, 1, 2, 1800.00m, 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Estado",
                table: "Appointments",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_FechaHora",
                table: "Appointments",
                column: "FechaHora");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_UserId",
                table: "Appointments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Activo",
                table: "Employees",
                column: "Activo");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Departamento",
                table: "Employees",
                column: "Departamento");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Email",
                table: "Employees",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Estado",
                table: "Invoices",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_NumeroFactura",
                table: "Invoices",
                column: "NumeroFactura",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_OrderId",
                table: "Invoices",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductId",
                table: "OrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Estado",
                table: "Orders",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_FechaPedido",
                table: "Orders",
                column: "FechaPedido",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId",
                table: "Orders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Activo",
                table: "Products",
                column: "Activo");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Categoria",
                table: "Products",
                column: "Categoria");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Nombre",
                table: "Products",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role",
                table: "Users",
                column: "Role");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
