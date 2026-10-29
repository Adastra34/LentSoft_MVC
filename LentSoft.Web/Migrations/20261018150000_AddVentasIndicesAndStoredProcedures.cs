using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LentSoft.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddVentasIndicesAndStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── SQL Server Stored Procedures ──
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE sp_RegistrarVentaProductos
    @OrderId INT,
    @ItemsJson NVARCHAR(MAX),
    @Responsable NVARCHAR(100) = 'Ventas'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM Orders WHERE Id = @OrderId)
        BEGIN
            THROW 50001, 'La orden especificada no existe.', 1;
        END

        CREATE TABLE #Items (
            ProductId INT,
            Cantidad INT,
            PrecioUnitario DECIMAL(18,2)
        );

        INSERT INTO #Items (ProductId, Cantidad, PrecioUnitario)
        SELECT ProductId, Cantidad, PrecioUnitario
        FROM OPENJSON(@ItemsJson)
        WITH (
            ProductId INT '$.ProductId',
            Cantidad INT '$.Cantidad',
            PrecioUnitario DECIMAL(18,2) '$.PrecioUnitario'
        );

        IF EXISTS (
            SELECT 1 
            FROM #Items i
            JOIN Products p ON p.Id = i.ProductId
            WHERE p.Stock < i.Cantidad OR p.Activo = 0
        )
        BEGIN
            THROW 50002, 'Stock insuficiente para uno o más productos de la venta.', 1;
        END

        INSERT INTO OrderItems (OrderId, ProductId, Cantidad, PrecioUnitario)
        SELECT @OrderId, i.ProductId, i.Cantidad, i.PrecioUnitario
        FROM #Items i;

        DECLARE @ProdId INT, @Cant INT, @Precio DECIMAL(18,2), @NuevoStock INT;
        DECLARE item_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT ProductId, Cantidad, PrecioUnitario FROM #Items;
        
        OPEN item_cursor;
        FETCH NEXT FROM item_cursor INTO @ProdId, @Cant, @Precio;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            UPDATE Products
            SET Stock = Stock - @Cant
            WHERE Id = @ProdId;

            SELECT @NuevoStock = Stock FROM Products WHERE Id = @ProdId;

            INSERT INTO InventoryMovements (ProductId, Tipo, Cantidad, Motivo, Nota, Referencia, StockResultante, Responsable, Fecha)
            VALUES (@ProdId, 'Salida', @Cant, 'Venta', 'Venta registrada desde módulo de ventas', CONCAT('ORD-', @OrderId), @NuevoStock, @Responsable, GETUTCDATE());

            FETCH NEXT FROM item_cursor INTO @ProdId, @Cant, @Precio;
        END

        CLOSE item_cursor;
        DEALLOCATE item_cursor;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;");

                migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE sp_ProductosVendidos
    @FechaInicio DATETIME = NULL,
    @FechaFin DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        p.Id AS ProductId,
        p.Nombre AS Producto,
        ISNULL(p.Sku, '') AS Sku,
        p.Categoria AS Categoria,
        SUM(oi.Cantidad) AS CantidadTotal,
        SUM(oi.Cantidad * oi.PrecioUnitario) AS TotalVentas
    FROM OrderItems oi
    INNER JOIN Orders o ON o.Id = oi.OrderId
    INNER JOIN Products p ON p.Id = oi.ProductId
    WHERE o.Activo = 1 
      AND o.Estado != 'cancelado'
      AND (@FechaInicio IS NULL OR o.FechaPedido >= @FechaInicio)
      AND (@FechaFin IS NULL OR o.FechaPedido <= @FechaFin)
    GROUP BY p.Id, p.Nombre, p.Sku, p.Categoria
    ORDER BY CantidadTotal DESC, TotalVentas DESC;
END;");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_RegistrarVentaProductos;");
                migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_ProductosVendidos;");
            }
        }
    }
}
