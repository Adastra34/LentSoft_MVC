using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LentSoft.Web.Data;

#nullable disable

namespace LentSoft.Web.Migrations
{
    [DbContext(typeof(LentSoftDbContext))]
    [Migration("20261127000000_SyncModelWithSqlServer")]
    public partial class SyncModelWithSqlServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                // ── 1. SalesOrders.ProductId (Separado en llamadas distintas) ──
                migrationBuilder.Sql(@"
IF COL_LENGTH('SalesOrders', 'ProductId') IS NULL
BEGIN
    ALTER TABLE [SalesOrders] ADD [ProductId] INT NULL;
END;");

                migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SalesOrders_Products_ProductId')
BEGIN
    ALTER TABLE [SalesOrders] ADD CONSTRAINT [FK_SalesOrders_Products_ProductId]
        FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION;
END;");

                migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SalesOrders_ProductId' AND object_id = OBJECT_ID('SalesOrders'))
BEGIN
    CREATE INDEX [IX_SalesOrders_ProductId] ON [SalesOrders] ([ProductId]);
END;");

                // ── 2. Tabla Pqrs y sus restricciones ──
                migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Pqrs]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Pqrs] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Tipo] NVARCHAR(50) NOT NULL,
        [Asunto] NVARCHAR(200) NOT NULL,
        [Mensaje] NVARCHAR(2000) NOT NULL,
        [Estado] NVARCHAR(50) NOT NULL,
        [FechaCreacion] DATETIME2 NOT NULL,
        [RespuestaAdmin] NVARCHAR(2000) NULL,
        [FechaRespuesta] DATETIME2 NULL,
        [UserId] INT NOT NULL,
        [Radicado] NVARCHAR(50) NULL,
        CONSTRAINT [PK_Pqrs] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Pqrs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;");

                migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Pqrs_UserId' AND object_id = OBJECT_ID('Pqrs'))
BEGIN
    CREATE INDEX [IX_Pqrs_UserId] ON [Pqrs] ([UserId]);
END;");

                // ── 3. Índices faltantes en InventoryMovements (presentes en Snapshot) ──
                migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InventoryMovements_ProductId_Fecha' AND object_id = OBJECT_ID('InventoryMovements'))
BEGIN
    CREATE INDEX [IX_InventoryMovements_ProductId_Fecha] ON [InventoryMovements] ([ProductId], [Fecha] DESC);
END;");

                migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InventoryMovements_Tipo_Fecha' AND object_id = OBJECT_ID('InventoryMovements'))
BEGIN
    CREATE INDEX [IX_InventoryMovements_Tipo_Fecha] ON [InventoryMovements] ([Tipo], [Fecha]);
END;");

                // ── 4. Índices faltantes en SupplierOrders (presentes en Snapshot) ──
                migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SupplierOrders_Estado' AND object_id = OBJECT_ID('SupplierOrders'))
BEGIN
    CREATE INDEX [IX_SupplierOrders_Estado] ON [SupplierOrders] ([Estado]);
END;");

                migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SupplierOrders_SupplierId_Estado' AND object_id = OBJECT_ID('SupplierOrders'))
BEGIN
    CREATE INDEX [IX_SupplierOrders_SupplierId_Estado] ON [SupplierOrders] ([SupplierId], [Estado]);
END;");

                migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SupplierOrders_ProductId_Estado' AND object_id = OBJECT_ID('SupplierOrders'))
BEGIN
    CREATE INDEX [IX_SupplierOrders_ProductId_Estado] ON [SupplierOrders] ([ProductId], [Estado]);
END;");
            }
            else if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS [Pqrs] (
    [Id] INTEGER PRIMARY KEY AUTOINCREMENT,
    [Tipo] TEXT NOT NULL,
    [Asunto] TEXT NOT NULL,
    [Mensaje] TEXT NOT NULL,
    [Estado] TEXT NOT NULL,
    [FechaCreacion] TEXT NOT NULL,
    [RespuestaAdmin] TEXT NULL,
    [FechaRespuesta] TEXT NULL,
    [UserId] INTEGER NOT NULL,
    [Radicado] TEXT NULL,
    FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS [IX_Pqrs_UserId] ON [Pqrs] ([UserId]);");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SupplierOrders_ProductId_Estado' AND object_id = OBJECT_ID('SupplierOrders')) DROP INDEX [IX_SupplierOrders_ProductId_Estado] ON [SupplierOrders];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SupplierOrders_SupplierId_Estado' AND object_id = OBJECT_ID('SupplierOrders')) DROP INDEX [IX_SupplierOrders_SupplierId_Estado] ON [SupplierOrders];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SupplierOrders_Estado' AND object_id = OBJECT_ID('SupplierOrders')) DROP INDEX [IX_SupplierOrders_Estado] ON [SupplierOrders];

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InventoryMovements_Tipo_Fecha' AND object_id = OBJECT_ID('InventoryMovements')) DROP INDEX [IX_InventoryMovements_Tipo_Fecha] ON [InventoryMovements];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InventoryMovements_ProductId_Fecha' AND object_id = OBJECT_ID('InventoryMovements')) DROP INDEX [IX_InventoryMovements_ProductId_Fecha] ON [InventoryMovements];

IF OBJECT_ID(N'[dbo].[Pqrs]', N'U') IS NOT NULL DROP TABLE [dbo].[Pqrs];

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SalesOrders_Products_ProductId') ALTER TABLE [SalesOrders] DROP CONSTRAINT [FK_SalesOrders_Products_ProductId];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SalesOrders_ProductId' AND object_id = OBJECT_ID('SalesOrders')) DROP INDEX [IX_SalesOrders_ProductId] ON [SalesOrders];
IF COL_LENGTH('SalesOrders', 'ProductId') IS NOT NULL ALTER TABLE [SalesOrders] DROP COLUMN [ProductId];");
            }
            else if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                migrationBuilder.Sql("DROP TABLE IF EXISTS [Pqrs];");
            }
        }
    }
}
