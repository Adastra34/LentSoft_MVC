using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LentSoft.Web.Migrations
{
    /// <inheritdoc />
    public partial class ModuloVentasStoredProceduresAndUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                // 1. Índice único en Invoices.NumeroFactura
                migrationBuilder.Sql(@"
                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Invoices_NumeroFactura' AND object_id = OBJECT_ID('Invoices'))
                        DROP INDEX IX_Invoices_NumeroFactura ON Invoices;
                    CREATE UNIQUE NONCLUSTERED INDEX IX_Invoices_NumeroFactura ON Invoices(NumeroFactura);
                ");

                // 2. Índice único en PagosVentas.NumeroComprobante
                migrationBuilder.Sql(@"
                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PagosVentas_NumeroComprobante' AND object_id = OBJECT_ID('PagosVentas') AND is_unique = 0)
                        DROP INDEX IX_PagosVentas_NumeroComprobante ON PagosVentas;
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PagosVentas_NumeroComprobante' AND object_id = OBJECT_ID('PagosVentas'))
                        CREATE UNIQUE NONCLUSTERED INDEX IX_PagosVentas_NumeroComprobante ON PagosVentas(NumeroComprobante);
                ");

                // 3. Stored Procedure: sp_RegistrarVentaProductos
                migrationBuilder.Sql(@"
                    CREATE OR ALTER PROCEDURE sp_RegistrarVentaProductos
                        @OrderId INT,
                        @ProductId INT,
                        @Cantidad INT,
                        @PrecioUnitario DECIMAL(18,2),
                        @Responsable NVARCHAR(100) = NULL,
                        @Nota NVARCHAR(300) = NULL
                    AS
                    BEGIN
                        SET NOCOUNT ON;
                        SET XACT_ABORT ON;

                        DECLARE @StockActual INT;
                        DECLARE @NombreProducto NVARCHAR(200);
                        DECLARE @Sku NVARCHAR(50);

                        SELECT @StockActual = Stock, @NombreProducto = Nombre, @Sku = Sku
                        FROM Products WITH (UPDLOCK, ROWLOCK)
                        WHERE Id = @ProductId;

                        IF @StockActual IS NULL
                        BEGIN
                            RAISERROR('El producto especificado no existe.', 16, 1);
                            RETURN;
                        END

                        IF @StockActual < @Cantidad
                        BEGIN
                            RAISERROR('Stock insuficiente para el producto.', 16, 1);
                            RETURN;
                        END

                        -- Descontar stock y desactivar si llega a 0
                        UPDATE Products
                        SET Stock = Stock - @Cantidad,
                            Activo = CASE WHEN Stock - @Cantidad <= 0 THEN 0 ELSE Activo END
                        WHERE Id = @ProductId;

                        -- Insertar ítem en OrderItems
                        INSERT INTO OrderItems (OrderId, ProductId, Cantidad, PrecioUnitario)
                        VALUES (@OrderId, @ProductId, @Cantidad, @PrecioUnitario);

                        -- Insertar movimiento en InventoryMovements con motivo 'Venta'
                        INSERT INTO InventoryMovements (ProductId, NombreProducto, Tipo, Cantidad, Fecha, Responsable, Motivo, StockResultante, Nota, Referencia)
                        VALUES (@ProductId, @NombreProducto, 'Salida', @Cantidad, GETUTCDATE(), @Responsable, 'Venta', @StockActual - @Cantidad, @Nota, @Sku);
                    END
                ");

                // 4. Stored Procedure: sp_ProductosVendidos
                migrationBuilder.Sql(@"
                    CREATE OR ALTER PROCEDURE sp_ProductosVendidos
                        @FechaInicio DATETIME = NULL,
                        @FechaFin DATETIME = NULL
                    AS
                    BEGIN
                        SET NOCOUNT ON;

                        SELECT 
                            oi.ProductId AS ProductoId,
                            p.Nombre AS ProductoNombre,
                            SUM(oi.Cantidad) AS CantidadVendida,
                            SUM(oi.Cantidad * oi.PrecioUnitario) AS TotalVendido
                        FROM OrderItems oi
                        INNER JOIN Orders o ON oi.OrderId = o.Id
                        INNER JOIN Products p ON oi.ProductId = p.Id
                        WHERE o.Activo = 1 AND o.Estado <> 'cancelado'
                          AND (@FechaInicio IS NULL OR o.FechaPedido >= @FechaInicio)
                          AND (@FechaFin IS NULL OR o.FechaPedido <= @FechaFin)
                        GROUP BY oi.ProductId, p.Nombre
                        ORDER BY CantidadVendida DESC;
                    END
                ");
            }
            else
            {
                // Proveedor SQLite (desarrollo local por defecto)
                migrationBuilder.Sql(@"
                    DROP INDEX IF EXISTS IX_Invoices_NumeroFactura;
                    CREATE UNIQUE INDEX IF NOT EXISTS IX_Invoices_NumeroFactura ON Invoices(NumeroFactura);
                    CREATE UNIQUE INDEX IF NOT EXISTS IX_PagosVentas_NumeroComprobante ON PagosVentas(NumeroComprobante);
                ");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_RegistrarVentaProductos;");
                migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_ProductosVendidos;");

                migrationBuilder.Sql(@"
                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Invoices_NumeroFactura' AND object_id = OBJECT_ID('Invoices'))
                    BEGIN
                        DROP INDEX IX_Invoices_NumeroFactura ON Invoices;
                        CREATE NONCLUSTERED INDEX IX_Invoices_NumeroFactura ON Invoices(NumeroFactura);
                    END
                ");
            }
            else
            {
                migrationBuilder.Sql(@"
                    DROP INDEX IF EXISTS IX_Invoices_NumeroFactura;
                    CREATE INDEX IF NOT EXISTS IX_Invoices_NumeroFactura ON Invoices(NumeroFactura);
                ");
            }
        }
    }
}
