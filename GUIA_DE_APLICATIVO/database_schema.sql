-- =============================================
-- Script de Creación de Base de Datos LentSoft
-- SQL Server 2019+
-- =============================================

USE master;
GO

-- Eliminar BD si existe (¡CUIDADO EN PRODUCCIÓN!)
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'LentSoftDB')
BEGIN
    ALTER DATABASE LentSoftDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE LentSoftDB;
END
GO

-- Crear base de datos
CREATE DATABASE LentSoftDB
COLLATE Modern_Spanish_CI_AS;
GO

USE LentSoftDB;
GO

-- =============================================
-- TABLA: Users (Usuarios)
-- =============================================
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    Telefono NVARCHAR(20) NULL,
    Role NVARCHAR(20) NOT NULL DEFAULT 'usuario',
    FechaRegistro DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UltimaCompra DATETIME2 NULL,

    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('usuario', 'admin'))
);
GO

CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_Role ON Users(Role);
GO

-- =============================================
-- TABLA: Products (Productos)
-- =============================================
CREATE TABLE Products (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(200) NOT NULL,
    Descripcion NVARCHAR(1000) NULL,
    Precio DECIMAL(10,2) NOT NULL,
    PrecioDescuento DECIMAL(10,2) NULL,
    Categoria NVARCHAR(50) NOT NULL,
    Marca NVARCHAR(50) NULL,
    Stock INT NOT NULL DEFAULT 0,
    ImagenUrl NVARCHAR(500) NULL,
    Activo BIT NOT NULL DEFAULT 1,
    FechaCreacion DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT CK_Products_Precio CHECK (Precio >= 0),
    CONSTRAINT CK_Products_Stock CHECK (Stock >= 0),
    CONSTRAINT CK_Products_PrecioDescuento CHECK (PrecioDescuento IS NULL OR PrecioDescuento < Precio)
);
GO

CREATE INDEX IX_Products_Categoria ON Products(Categoria);
CREATE INDEX IX_Products_Nombre ON Products(Nombre);
CREATE INDEX IX_Products_Activo ON Products(Activo);
GO

-- =============================================
-- TABLA: Orders (Pedidos)
-- =============================================
CREATE TABLE Orders (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Total DECIMAL(10,2) NOT NULL,
    Estado NVARCHAR(20) NOT NULL DEFAULT 'pendiente',
    DireccionEnvio NVARCHAR(500) NULL,
    FechaPedido DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FechaEntrega DATETIME2 NULL,

    CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId)
        REFERENCES Users(Id) ON DELETE NO ACTION,
    CONSTRAINT CK_Orders_Total CHECK (Total >= 0),
    CONSTRAINT CK_Orders_Estado CHECK (Estado IN ('pendiente', 'procesando', 'enviado', 'entregado', 'cancelado'))
);
GO

CREATE INDEX IX_Orders_UserId ON Orders(UserId);
CREATE INDEX IX_Orders_Estado ON Orders(Estado);
CREATE INDEX IX_Orders_FechaPedido ON Orders(FechaPedido DESC);
GO

-- =============================================
-- TABLA: OrderItems (Items de Pedido)
-- =============================================
CREATE TABLE OrderItems (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    Cantidad INT NOT NULL,
    PrecioUnitario DECIMAL(10,2) NOT NULL,

    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId)
        REFERENCES Orders(Id) ON DELETE CASCADE,
    CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId)
        REFERENCES Products(Id) ON DELETE NO ACTION,
    CONSTRAINT CK_OrderItems_Cantidad CHECK (Cantidad > 0),
    CONSTRAINT CK_OrderItems_PrecioUnitario CHECK (PrecioUnitario >= 0)
);
GO

CREATE INDEX IX_OrderItems_OrderId ON OrderItems(OrderId);
CREATE INDEX IX_OrderItems_ProductId ON OrderItems(ProductId);
GO

-- =============================================
-- TABLA: Employees (Empleados)
-- =============================================
CREATE TABLE Employees (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    Telefono NVARCHAR(20) NULL,
    Puesto NVARCHAR(50) NOT NULL,
    Departamento NVARCHAR(50) NOT NULL,
    Salario DECIMAL(10,2) NOT NULL,
    FechaContratacion DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Activo BIT NOT NULL DEFAULT 1,

    CONSTRAINT UQ_Employees_Email UNIQUE (Email),
    CONSTRAINT CK_Employees_Salario CHECK (Salario >= 0)
);
GO

CREATE INDEX IX_Employees_Email ON Employees(Email);
CREATE INDEX IX_Employees_Departamento ON Employees(Departamento);
CREATE INDEX IX_Employees_Activo ON Employees(Activo);
GO

-- =============================================
-- TABLA: Invoices (Facturas)
-- =============================================
CREATE TABLE Invoices (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NumeroFactura NVARCHAR(50) NOT NULL,
    OrderId INT NOT NULL,
    Subtotal DECIMAL(10,2) NOT NULL,
    Impuestos DECIMAL(10,2) NOT NULL,
    Total DECIMAL(10,2) NOT NULL,
    Estado NVARCHAR(20) NOT NULL DEFAULT 'pendiente',
    FechaEmision DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FechaPago DATETIME2 NULL,
    MetodoPago NVARCHAR(50) NULL,

    CONSTRAINT UQ_Invoices_NumeroFactura UNIQUE (NumeroFactura),
    CONSTRAINT FK_Invoices_Orders FOREIGN KEY (OrderId)
        REFERENCES Orders(Id) ON DELETE CASCADE,
    CONSTRAINT CK_Invoices_Subtotal CHECK (Subtotal >= 0),
    CONSTRAINT CK_Invoices_Impuestos CHECK (Impuestos >= 0),
    CONSTRAINT CK_Invoices_Total CHECK (Total >= 0),
    CONSTRAINT CK_Invoices_Estado CHECK (Estado IN ('pendiente', 'pagada', 'cancelada'))
);
GO

CREATE INDEX IX_Invoices_OrderId ON Invoices(OrderId);
CREATE INDEX IX_Invoices_NumeroFactura ON Invoices(NumeroFactura);
CREATE INDEX IX_Invoices_Estado ON Invoices(Estado);
GO

-- =============================================
-- TABLA: Appointments (Citas)
-- =============================================
CREATE TABLE Appointments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Servicio NVARCHAR(100) NOT NULL,
    FechaHora DATETIME2 NOT NULL,
    Estado NVARCHAR(20) NOT NULL DEFAULT 'pendiente',
    Notas NVARCHAR(500) NULL,
    FechaCreacion DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_Appointments_Users FOREIGN KEY (UserId)
        REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT CK_Appointments_Estado CHECK (Estado IN ('pendiente', 'confirmada', 'completada', 'cancelada'))
);
GO

CREATE INDEX IX_Appointments_UserId ON Appointments(UserId);
CREATE INDEX IX_Appointments_FechaHora ON Appointments(FechaHora);
CREATE INDEX IX_Appointments_Estado ON Appointments(Estado);
GO

-- =============================================
-- DATOS INICIALES (SEED DATA)
-- =============================================

-- Usuario Administrador
INSERT INTO Users (Nombre, Email, PasswordHash, Role, FechaRegistro)
VALUES
    ('Administrador', 'admin@lentsoft.com', '$2a$11$XYZ...', 'admin', '2026-01-01'),
    ('Usuario Demo', 'user@lentsoft.com', '$2a$11$ABC...', 'usuario', '2026-01-01');
GO

-- Productos de ejemplo
INSERT INTO Products (Nombre, Descripcion, Precio, PrecioDescuento, Categoria, Marca, Stock, ImagenUrl, Activo)
VALUES
    ('Lentes Ray-Ban Aviator', 'Lentes de sol clásicos estilo aviador', 2500.00, NULL, 'lentes-sol', 'Ray-Ban', 50, 'https://images.unsplash.com/photo-1572635196237-14b3f281503f', 1),
    ('Lentes de Contacto Acuvue', 'Lentes de contacto mensuales', 450.00, 399.00, 'lentes-contacto', 'Acuvue', 100, NULL, 1),
    ('Montura Oakley Sport', 'Montura deportiva ultraligera', 1800.00, NULL, 'monturas', 'Oakley', 30, NULL, 1),
    ('Lentes Graduados Classic', 'Lentes graduados con diseño clásico', 1200.00, NULL, 'lentes-graduados', 'LentSoft', 40, NULL, 1),
    ('Estuche Premium', 'Estuche rígido para lentes', 150.00, 99.00, 'accesorios', 'LentSoft', 200, NULL, 1),
    ('Líquido Limpiador', 'Solución limpiadora para lentes 360ml', 120.00, NULL, 'accesorios', 'Opti-Free', 150, NULL, 1);
GO

-- Empleados de ejemplo
INSERT INTO Employees (Nombre, Email, Telefono, Puesto, Departamento, Salario, FechaContratacion, Activo)
VALUES
    ('María García', 'maria.garcia@lentsoft.com', '555-0101', 'Optometrista', 'Atención al Cliente', 25000.00, '2025-06-01', 1),
    ('Juan Pérez', 'juan.perez@lentsoft.com', '555-0102', 'Vendedor', 'Ventas', 18000.00, '2025-08-15', 1),
    ('Ana Martínez', 'ana.martinez@lentsoft.com', '555-0103', 'Gerente', 'Administración', 35000.00, '2024-03-01', 1);
GO

-- =============================================
-- VISTAS ÚTILES
-- =============================================

-- Vista de productos con descuento
CREATE VIEW vw_ProductosConDescuento AS
SELECT
    Id,
    Nombre,
    Precio,
    PrecioDescuento,
    CAST(((Precio - PrecioDescuento) / Precio * 100) AS INT) AS PorcentajeDescuento,
    Categoria,
    Marca,
    Stock
FROM Products
WHERE PrecioDescuento IS NOT NULL
  AND PrecioDescuento < Precio
  AND Activo = 1;
GO

-- Vista de pedidos con detalles de usuario
CREATE VIEW vw_PedidosCompletos AS
SELECT
    o.Id AS PedidoId,
    o.FechaPedido,
    o.Estado,
    o.Total,
    u.Id AS UserId,
    u.Nombre AS NombreUsuario,
    u.Email AS EmailUsuario,
    COUNT(oi.Id) AS CantidadItems
FROM Orders o
INNER JOIN Users u ON o.UserId = u.Id
LEFT JOIN OrderItems oi ON o.Id = oi.OrderId
GROUP BY o.Id, o.FechaPedido, o.Estado, o.Total, u.Id, u.Nombre, u.Email;
GO

-- Vista de estadísticas de ventas
CREATE VIEW vw_EstadisticasVentas AS
SELECT
    YEAR(FechaPedido) AS Año,
    MONTH(FechaPedido) AS Mes,
    COUNT(*) AS TotalPedidos,
    SUM(Total) AS TotalVentas,
    AVG(Total) AS PromedioVenta
FROM Orders
WHERE Estado != 'cancelado'
GROUP BY YEAR(FechaPedido), MONTH(FechaPedido);
GO

-- =============================================
-- PROCEDIMIENTOS ALMACENADOS
-- =============================================

-- SP: Crear pedido completo
CREATE PROCEDURE sp_CrearPedido
    @UserId INT,
    @DireccionEnvio NVARCHAR(500),
    @Items NVARCHAR(MAX) -- JSON array de items
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    BEGIN TRY
        DECLARE @OrderId INT;
        DECLARE @Total DECIMAL(10,2) = 0;

        -- Crear el pedido
        INSERT INTO Orders (UserId, Total, Estado, DireccionEnvio)
        VALUES (@UserId, 0, 'pendiente', @DireccionEnvio);

        SET @OrderId = SCOPE_IDENTITY();

        -- Insertar items (requiere SQL Server 2016+)
        INSERT INTO OrderItems (OrderId, ProductId, Cantidad, PrecioUnitario)
        SELECT
            @OrderId,
            JSON_VALUE(value, '$.productId'),
            JSON_VALUE(value, '$.cantidad'),
            p.Precio
        FROM OPENJSON(@Items)
        CROSS APPLY (
            SELECT Precio FROM Products
            WHERE Id = JSON_VALUE(value, '$.productId')
        ) p;

        -- Calcular total
        SELECT @Total = SUM(Cantidad * PrecioUnitario)
        FROM OrderItems
        WHERE OrderId = @OrderId;

        -- Actualizar total del pedido
        UPDATE Orders
        SET Total = @Total
        WHERE Id = @OrderId;

        -- Actualizar stock
        UPDATE p
        SET p.Stock = p.Stock - oi.Cantidad
        FROM Products p
        INNER JOIN OrderItems oi ON p.Id = oi.ProductId
        WHERE oi.OrderId = @OrderId;

        COMMIT TRANSACTION;

        -- Retornar el pedido creado
        SELECT * FROM Orders WHERE Id = @OrderId;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- SP: Obtener dashboard de administrador
CREATE PROCEDURE sp_DashboardAdmin
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        (SELECT COUNT(*) FROM Users WHERE Role = 'usuario') AS TotalUsuarios,
        (SELECT COUNT(*) FROM Products WHERE Activo = 1) AS TotalProductos,
        (SELECT COUNT(*) FROM Orders WHERE Estado != 'cancelado') AS TotalPedidos,
        (SELECT ISNULL(SUM(Total), 0) FROM Orders WHERE Estado != 'cancelado') AS VentasTotales,
        (SELECT COUNT(*) FROM Orders WHERE Estado = 'pendiente') AS PedidosPendientes,
        (SELECT COUNT(*) FROM Employees WHERE Activo = 1) AS TotalEmpleados,
        (SELECT COUNT(*) FROM Appointments WHERE Estado = 'pendiente') AS CitasPendientes;
END;
GO

-- =============================================
-- TRIGGERS
-- =============================================

-- Trigger: Actualizar UltimaCompra del usuario
CREATE TRIGGER trg_UpdateUltimaCompra
ON Orders
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE u
    SET u.UltimaCompra = i.FechaPedido
    FROM Users u
    INNER JOIN inserted i ON u.Id = i.UserId
    WHERE i.Estado = 'entregado';
END;
GO

-- Trigger: Generar número de factura automático
CREATE TRIGGER trg_GenerateInvoiceNumber
ON Invoices
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Year VARCHAR(4) = YEAR(GETUTCDATE());
    DECLARE @NextNumber INT;

    SELECT @NextNumber = ISNULL(MAX(CAST(SUBSTRING(NumeroFactura, 6, 10) AS INT)), 0) + 1
    FROM Invoices
    WHERE NumeroFactura LIKE @Year + '%';

    INSERT INTO Invoices (NumeroFactura, OrderId, Subtotal, Impuestos, Total, Estado, FechaEmision, FechaPago, MetodoPago)
    SELECT
        @Year + '-' + RIGHT('000000' + CAST(@NextNumber AS VARCHAR), 6),
        OrderId,
        Subtotal,
        Impuestos,
        Total,
        Estado,
        FechaEmision,
        FechaPago,
        MetodoPago
    FROM inserted;
END;
GO

-- =============================================
-- PERMISOS Y SEGURIDAD
-- =============================================

-- Crear rol de solo lectura
-- CREATE ROLE LentSoft_ReadOnly;
-- GRANT SELECT ON SCHEMA::dbo TO LentSoft_ReadOnly;

-- Crear rol de aplicación
-- CREATE ROLE LentSoft_App;
-- GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO LentSoft_App;

PRINT 'Base de datos LentSoftDB creada exitosamente';
PRINT 'Tablas: 7';
PRINT 'Vistas: 3';
PRINT 'Procedimientos: 2';
PRINT 'Triggers: 2';
GO
