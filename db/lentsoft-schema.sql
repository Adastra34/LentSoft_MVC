IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    CREATE TABLE [Employees] (
        [Id] int NOT NULL IDENTITY,
        [Nombre] nvarchar(100) NOT NULL,
        [Email] nvarchar(100) NOT NULL,
        [Telefono] nvarchar(20) NULL,
        [Puesto] nvarchar(50) NOT NULL,
        [Departamento] nvarchar(50) NOT NULL,
        [Salario] decimal(10,2) NOT NULL,
        [FechaContratacion] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_Employees] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    CREATE TABLE [Products] (
        [Id] int NOT NULL IDENTITY,
        [Nombre] nvarchar(200) NOT NULL,
        [Descripcion] nvarchar(1000) NULL,
        [Precio] decimal(10,2) NOT NULL,
        [PrecioDescuento] decimal(10,2) NULL,
        [Categoria] nvarchar(50) NOT NULL,
        [Marca] nvarchar(50) NULL,
        [Stock] int NOT NULL DEFAULT 0,
        [ImagenUrl] nvarchar(500) NULL,
        [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [FechaCreacion] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [Nombre] nvarchar(100) NOT NULL,
        [Email] nvarchar(100) NOT NULL,
        [PasswordHash] nvarchar(255) NOT NULL,
        [Telefono] nvarchar(20) NULL,
        [Role] nvarchar(20) NOT NULL DEFAULT N'usuario',
        [FechaRegistro] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UltimaCompra] datetime2 NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    CREATE TABLE [Appointments] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Servicio] nvarchar(100) NOT NULL,
        [FechaHora] datetime2 NOT NULL,
        [Estado] nvarchar(20) NOT NULL DEFAULT N'pendiente',
        [Notas] nvarchar(500) NULL,
        [FechaCreacion] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_Appointments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Appointments_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    CREATE TABLE [Orders] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Total] decimal(10,2) NOT NULL,
        [Estado] nvarchar(20) NOT NULL DEFAULT N'pendiente',
        [DireccionEnvio] nvarchar(500) NULL,
        [FechaPedido] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [FechaEntrega] datetime2 NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Orders_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    CREATE TABLE [Invoices] (
        [Id] int NOT NULL IDENTITY,
        [NumeroFactura] nvarchar(50) NOT NULL,
        [OrderId] int NOT NULL,
        [Subtotal] decimal(10,2) NOT NULL,
        [Impuestos] decimal(10,2) NOT NULL,
        [Total] decimal(10,2) NOT NULL,
        [Estado] nvarchar(20) NOT NULL DEFAULT N'pendiente',
        [FechaEmision] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [FechaPago] datetime2 NULL,
        [MetodoPago] nvarchar(50) NULL,
        CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Invoices_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    CREATE TABLE [OrderItems] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [ProductId] int NOT NULL,
        [Cantidad] int NOT NULL,
        [PrecioUnitario] decimal(10,2) NOT NULL,
        CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OrderItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Departamento', N'Email', N'FechaContratacion', N'Nombre', N'Puesto', N'Salario', N'Telefono') AND [object_id] = OBJECT_ID(N'[Employees]'))
        SET IDENTITY_INSERT [Employees] ON;
    EXEC(N'INSERT INTO [Employees] ([Id], [Activo], [Departamento], [Email], [FechaContratacion], [Nombre], [Puesto], [Salario], [Telefono])
    VALUES (1, CAST(1 AS bit), N''Atención al Cliente'', N''maria.garcia@lentsoft.com'', ''2025-06-01T00:00:00.0000000Z'', N''María García'', N''Optometrista'', 25000.0, N''555-0101''),
    (2, CAST(1 AS bit), N''Ventas'', N''juan.perez@lentsoft.com'', ''2025-08-15T00:00:00.0000000Z'', N''Juan Pérez'', N''Vendedor'', 18000.0, N''555-0102''),
    (3, CAST(1 AS bit), N''Administración'', N''ana.martinez@lentsoft.com'', ''2024-03-01T00:00:00.0000000Z'', N''Ana Martínez'', N''Gerente'', 35000.0, N''555-0103'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Departamento', N'Email', N'FechaContratacion', N'Nombre', N'Puesto', N'Salario', N'Telefono') AND [object_id] = OBJECT_ID(N'[Employees]'))
        SET IDENTITY_INSERT [Employees] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Categoria', N'Descripcion', N'FechaCreacion', N'ImagenUrl', N'Marca', N'Nombre', N'Precio', N'PrecioDescuento', N'Stock') AND [object_id] = OBJECT_ID(N'[Products]'))
        SET IDENTITY_INSERT [Products] ON;
    EXEC(N'INSERT INTO [Products] ([Id], [Activo], [Categoria], [Descripcion], [FechaCreacion], [ImagenUrl], [Marca], [Nombre], [Precio], [PrecioDescuento], [Stock])
    VALUES (1, CAST(1 AS bit), N''lentes-sol'', N''Lentes de sol clásicos estilo aviador'', ''2026-01-01T00:00:00.0000000Z'', N''https://images.unsplash.com/photo-1572635196237-14b3f281503f'', N''Ray-Ban'', N''Lentes Ray-Ban Aviator'', 2500.0, NULL, 50),
    (2, CAST(1 AS bit), N''lentes-contacto'', N''Lentes de contacto mensuales'', ''2026-01-01T00:00:00.0000000Z'', NULL, N''Acuvue'', N''Lentes de Contacto Acuvue'', 450.0, 399.0, 100),
    (3, CAST(1 AS bit), N''monturas'', N''Montura deportiva ultraligera'', ''2026-01-01T00:00:00.0000000Z'', NULL, N''Oakley'', N''Montura Oakley Sport'', 1800.0, NULL, 30),
    (4, CAST(1 AS bit), N''lentes-graduados'', N''Lentes graduados con diseño clásico'', ''2026-01-01T00:00:00.0000000Z'', NULL, N''LentSoft'', N''Lentes Graduados Classic'', 1200.0, NULL, 40),
    (5, CAST(1 AS bit), N''accesorios'', N''Estuche rígido para lentes'', ''2026-01-01T00:00:00.0000000Z'', NULL, N''LentSoft'', N''Estuche Premium'', 150.0, 99.0, 200),
    (6, CAST(1 AS bit), N''accesorios'', N''Solución limpiadora para lentes 360ml'', ''2026-01-01T00:00:00.0000000Z'', NULL, N''Opti-Free'', N''Líquido Limpiador'', 120.0, NULL, 150)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Categoria', N'Descripcion', N'FechaCreacion', N'ImagenUrl', N'Marca', N'Nombre', N'Precio', N'PrecioDescuento', N'Stock') AND [object_id] = OBJECT_ID(N'[Products]'))
        SET IDENTITY_INSERT [Products] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Email', N'FechaRegistro', N'Nombre', N'PasswordHash', N'Role', N'Telefono', N'UltimaCompra') AND [object_id] = OBJECT_ID(N'[Users]'))
        SET IDENTITY_INSERT [Users] ON;
    EXEC(N'INSERT INTO [Users] ([Id], [Email], [FechaRegistro], [Nombre], [PasswordHash], [Role], [Telefono], [UltimaCompra])
    VALUES (1, N''admin@lentsoft.com'', ''2026-01-01T00:00:00.0000000Z'', N''Administrador'', N''$2a$11$MJPUqK7jAM6tEvUkExo1cO/3cmh4MpxnXNVPg./4kKzlsqAwPW/oq'', N''admin'', NULL, NULL),
    (2, N''user@lentsoft.com'', ''2026-01-01T00:00:00.0000000Z'', N''Usuario Demo'', N''$2a$11$q43GcbtmtTn9FyysOC73SO4HUFfBAF43GzPuZ6y0d0EZeDitCKqGa'', N''usuario'', NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Email', N'FechaRegistro', N'Nombre', N'PasswordHash', N'Role', N'Telefono', N'UltimaCompra') AND [object_id] = OBJECT_ID(N'[Users]'))
        SET IDENTITY_INSERT [Users] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Estado', N'FechaCreacion', N'FechaHora', N'Notas', N'Servicio', N'UserId') AND [object_id] = OBJECT_ID(N'[Appointments]'))
        SET IDENTITY_INSERT [Appointments] ON;
    EXEC(N'INSERT INTO [Appointments] ([Id], [Estado], [FechaCreacion], [FechaHora], [Notas], [Servicio], [UserId])
    VALUES (1, N''confirmada'', ''2026-05-10T00:00:00.0000000Z'', ''2026-05-25T10:00:00.0000000Z'', NULL, N''Examen de vista'', 2),
    (2, N''pendiente'', ''2026-05-15T00:00:00.0000000Z'', ''2026-06-02T14:30:00.0000000Z'', NULL, N''Ajuste de lentes'', 2)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Estado', N'FechaCreacion', N'FechaHora', N'Notas', N'Servicio', N'UserId') AND [object_id] = OBJECT_ID(N'[Appointments]'))
        SET IDENTITY_INSERT [Appointments] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DireccionEnvio', N'Estado', N'FechaEntrega', N'FechaPedido', N'Total', N'UserId') AND [object_id] = OBJECT_ID(N'[Orders]'))
        SET IDENTITY_INSERT [Orders] ON;
    EXEC(N'INSERT INTO [Orders] ([Id], [DireccionEnvio], [Estado], [FechaEntrega], [FechaPedido], [Total], [UserId])
    VALUES (1, N''Calle 123 #45-67'', N''entregado'', NULL, ''2026-05-15T10:00:00.0000000Z'', 2500.0, 2),
    (2, N''Calle 123 #45-67'', N''enviado'', NULL, ''2026-05-20T14:30:00.0000000Z'', 1800.0, 2)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DireccionEnvio', N'Estado', N'FechaEntrega', N'FechaPedido', N'Total', N'UserId') AND [object_id] = OBJECT_ID(N'[Orders]'))
        SET IDENTITY_INSERT [Orders] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Cantidad', N'OrderId', N'PrecioUnitario', N'ProductId') AND [object_id] = OBJECT_ID(N'[OrderItems]'))
        SET IDENTITY_INSERT [OrderItems] ON;
    EXEC(N'INSERT INTO [OrderItems] ([Id], [Cantidad], [OrderId], [PrecioUnitario], [ProductId])
    VALUES (1, 1, 1, 2500.0, 1),
    (2, 1, 2, 1800.0, 3)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Cantidad', N'OrderId', N'PrecioUnitario', N'ProductId') AND [object_id] = OBJECT_ID(N'[OrderItems]'))
        SET IDENTITY_INSERT [OrderItems] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Appointments_Estado] ON [Appointments] ([Estado]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Appointments_FechaHora] ON [Appointments] ([FechaHora]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Appointments_UserId] ON [Appointments] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_Activo] ON [Employees] ([Activo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_Departamento] ON [Employees] ([Departamento]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Employees_Email] ON [Employees] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Invoices_Estado] ON [Invoices] ([Estado]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Invoices_NumeroFactura] ON [Invoices] ([NumeroFactura]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Invoices_OrderId] ON [Invoices] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrderItems_ProductId] ON [OrderItems] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Orders_Estado] ON [Orders] ([Estado]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Orders_FechaPedido] ON [Orders] ([FechaPedido] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Orders_UserId] ON [Orders] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Products_Activo] ON [Products] ([Activo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Products_Categoria] ON [Products] ([Categoria]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Products_Nombre] ON [Products] ([Nombre]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_Role] ON [Users] ([Role]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200931_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260722200931_InitialCreate', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722202211_UpdateRolesConstraint'
)
BEGIN
    EXEC(N'ALTER TABLE [Users] ADD CONSTRAINT [CK_Users_Role] CHECK ([Role] IN (''usuario'', ''admin'', ''optometra'', ''ventas''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722202211_UpdateRolesConstraint'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260722202211_UpdateRolesConstraint', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722205810_AddOptometraAndVentasSeedUsers'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Email', N'FechaRegistro', N'Nombre', N'PasswordHash', N'Role', N'Telefono', N'UltimaCompra') AND [object_id] = OBJECT_ID(N'[Users]'))
        SET IDENTITY_INSERT [Users] ON;
    EXEC(N'INSERT INTO [Users] ([Id], [Email], [FechaRegistro], [Nombre], [PasswordHash], [Role], [Telefono], [UltimaCompra])
    VALUES (3, N''optometra@lentsoft.com'', ''2026-01-01T00:00:00.0000000Z'', N''Dra. María García'', N''$2a$11$MJPUqK7jAM6tEvUkExo1cO/3cmh4MpxnXNVPg./4kKzlsqAwPW/oq'', N''optometra'', NULL, NULL),
    (4, N''ventas@lentsoft.com'', ''2026-01-01T00:00:00.0000000Z'', N''Juan Pérez (Ventas)'', N''$2a$11$MJPUqK7jAM6tEvUkExo1cO/3cmh4MpxnXNVPg./4kKzlsqAwPW/oq'', N''ventas'', NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Email', N'FechaRegistro', N'Nombre', N'PasswordHash', N'Role', N'Telefono', N'UltimaCompra') AND [object_id] = OBJECT_ID(N'[Users]'))
        SET IDENTITY_INSERT [Users] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722205810_AddOptometraAndVentasSeedUsers'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260722205810_AddOptometraAndVentasSeedUsers', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722221700_AddUserIdentityFields'
)
BEGIN
    ALTER TABLE [Users] ADD [Apellido] nvarchar(100) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722221700_AddUserIdentityFields'
)
BEGIN
    ALTER TABLE [Users] ADD [NumeroDocumento] nvarchar(30) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722221700_AddUserIdentityFields'
)
BEGIN
    ALTER TABLE [Users] ADD [TipoDocumento] nvarchar(20) NOT NULL DEFAULT N'CC';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722221700_AddUserIdentityFields'
)
BEGIN
    EXEC(N'UPDATE [Users] SET [Apellido] = N''Sistema'', [NumeroDocumento] = N''1000000001'', [TipoDocumento] = N''CC''
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722221700_AddUserIdentityFields'
)
BEGIN
    EXEC(N'UPDATE [Users] SET [Apellido] = N''Demo'', [Nombre] = N''Usuario'', [NumeroDocumento] = N''1000000002'', [TipoDocumento] = N''CC''
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722221700_AddUserIdentityFields'
)
BEGIN
    EXEC(N'UPDATE [Users] SET [Apellido] = N''García'', [Nombre] = N''María'', [NumeroDocumento] = N''1000000003'', [TipoDocumento] = N''CC''
    WHERE [Id] = 3;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722221700_AddUserIdentityFields'
)
BEGIN
    EXEC(N'UPDATE [Users] SET [Apellido] = N''Pérez'', [Nombre] = N''Juan'', [NumeroDocumento] = N''1000000004'', [TipoDocumento] = N''CC''
    WHERE [Id] = 4;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722221700_AddUserIdentityFields'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_NumeroDocumento] ON [Users] ([NumeroDocumento]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722221700_AddUserIdentityFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260722221700_AddUserIdentityFields', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722223423_AddProductRatingAndFavorites'
)
BEGIN
    ALTER TABLE [Products] ADD [EsDestacado] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722223423_AddProductRatingAndFavorites'
)
BEGIN
    ALTER TABLE [Products] ADD [Rating] decimal(3,2) NOT NULL DEFAULT 4.8;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722223423_AddProductRatingAndFavorites'
)
BEGIN
    ALTER TABLE [Products] ADD [ReviewCount] int NOT NULL DEFAULT 12;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722223423_AddProductRatingAndFavorites'
)
BEGIN
    CREATE TABLE [Favorites] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [ProductId] int NOT NULL,
        [FechaAgregado] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_Favorites] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Favorites_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Favorites_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722223423_AddProductRatingAndFavorites'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'FechaAgregado', N'ProductId', N'UserId') AND [object_id] = OBJECT_ID(N'[Favorites]'))
        SET IDENTITY_INSERT [Favorites] ON;
    EXEC(N'INSERT INTO [Favorites] ([Id], [FechaAgregado], [ProductId], [UserId])
    VALUES (1, ''2026-01-01T00:00:00.0000000Z'', 1, 2),
    (2, ''2026-01-01T00:00:00.0000000Z'', 2, 2)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'FechaAgregado', N'ProductId', N'UserId') AND [object_id] = OBJECT_ID(N'[Favorites]'))
        SET IDENTITY_INSERT [Favorites] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722223423_AddProductRatingAndFavorites'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [EsDestacado] = CAST(1 AS bit), [Rating] = 4.9, [ReviewCount] = 28
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722223423_AddProductRatingAndFavorites'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [EsDestacado] = CAST(1 AS bit), [Rating] = 4.7, [ReviewCount] = 42
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722223423_AddProductRatingAndFavorites'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [EsDestacado] = CAST(1 AS bit), [Rating] = 4.8, [ReviewCount] = 15
    WHERE [Id] = 3;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722223423_AddProductRatingAndFavorites'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [EsDestacado] = CAST(1 AS bit), [Rating] = 4.6, [ReviewCount] = 19
    WHERE [Id] = 4;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722223423_AddProductRatingAndFavorites'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [Rating] = 4.5, [ReviewCount] = 8
    WHERE [Id] = 5;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722223423_AddProductRatingAndFavorites'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [Rating] = 4.9, [ReviewCount] = 33
    WHERE [Id] = 6;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722223423_AddProductRatingAndFavorites'
)
BEGIN
    CREATE INDEX [IX_Products_EsDestacado] ON [Products] ([EsDestacado]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722223423_AddProductRatingAndFavorites'
)
BEGIN
    CREATE INDEX [IX_Favorites_ProductId] ON [Favorites] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722223423_AddProductRatingAndFavorites'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Favorites_UserId_ProductId] ON [Favorites] ([UserId], [ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722223423_AddProductRatingAndFavorites'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260722223423_AddProductRatingAndFavorites', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727203721_AddProductDetailsFields'
)
BEGIN
    ALTER TABLE [Products] ADD [Color] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727203721_AddProductDetailsFields'
)
BEGIN
    ALTER TABLE [Products] ADD [Estilo] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727203721_AddProductDetailsFields'
)
BEGIN
    ALTER TABLE [Products] ADD [Material] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727203721_AddProductDetailsFields'
)
BEGIN
    ALTER TABLE [Products] ADD [Proteccion] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727203721_AddProductDetailsFields'
)
BEGIN
    ALTER TABLE [Products] ADD [Tamanio] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727203721_AddProductDetailsFields'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [Color] = N''Negro / Verde G-15'', [Estilo] = N''Aviador'', [Material] = N''Metal'', [Proteccion] = N''UV400'', [Tamanio] = N''58-14-135''
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727203721_AddProductDetailsFields'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [Color] = NULL, [Estilo] = NULL, [Material] = NULL, [Proteccion] = NULL, [Tamanio] = NULL
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727203721_AddProductDetailsFields'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [Color] = N''Negro Mate'', [Estilo] = N''Deportivo'', [Material] = N''O-Matter (Plástico)'', [Proteccion] = N''Filtro UV'', [Tamanio] = N''55-18-140''
    WHERE [Id] = 3;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727203721_AddProductDetailsFields'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [Color] = N''Carey'', [Estilo] = N''Wayfarer'', [Material] = N''Acetato'', [Proteccion] = N''Antirreflejo / Luz Azul'', [Tamanio] = N''52-19-145''
    WHERE [Id] = 4;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727203721_AddProductDetailsFields'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [Color] = NULL, [Estilo] = NULL, [Material] = NULL, [Proteccion] = NULL, [Tamanio] = NULL
    WHERE [Id] = 5;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727203721_AddProductDetailsFields'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [Color] = NULL, [Estilo] = NULL, [Material] = NULL, [Proteccion] = NULL, [Tamanio] = NULL
    WHERE [Id] = 6;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727203721_AddProductDetailsFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260727203721_AddProductDetailsFields', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727205214_AddShoppingCart'
)
BEGIN
    ALTER TABLE [Orders] ADD [MetodoPagoSimulado] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727205214_AddShoppingCart'
)
BEGIN
    CREATE TABLE [Carts] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [FechaCreacion] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_Carts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Carts_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727205214_AddShoppingCart'
)
BEGIN
    CREATE TABLE [CartItems] (
        [Id] int NOT NULL IDENTITY,
        [CartId] int NOT NULL,
        [ProductId] int NOT NULL,
        [Cantidad] int NOT NULL,
        [PrecioUnitario] decimal(10,2) NOT NULL,
        CONSTRAINT [PK_CartItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CartItems_Carts_CartId] FOREIGN KEY ([CartId]) REFERENCES [Carts] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CartItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727205214_AddShoppingCart'
)
BEGIN
    EXEC(N'UPDATE [Orders] SET [MetodoPagoSimulado] = NULL
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727205214_AddShoppingCart'
)
BEGIN
    EXEC(N'UPDATE [Orders] SET [MetodoPagoSimulado] = NULL
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727205214_AddShoppingCart'
)
BEGIN
    CREATE INDEX [IX_CartItems_CartId] ON [CartItems] ([CartId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727205214_AddShoppingCart'
)
BEGIN
    CREATE INDEX [IX_CartItems_ProductId] ON [CartItems] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727205214_AddShoppingCart'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Carts_UserId] ON [Carts] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727205214_AddShoppingCart'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260727205214_AddShoppingCart', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727221436_AddProductOverlayField'
)
BEGIN
    ALTER TABLE [Products] ADD [ImagenOverlayUrl] nvarchar(255) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727221436_AddProductOverlayField'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [ImagenOverlayUrl] = N''/img/overlays/rayban_aviator.png''
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727221436_AddProductOverlayField'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [ImagenOverlayUrl] = NULL
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727221436_AddProductOverlayField'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [ImagenOverlayUrl] = N''/img/overlays/oakley_sport.png''
    WHERE [Id] = 3;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727221436_AddProductOverlayField'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [ImagenOverlayUrl] = N''/img/overlays/classic.png''
    WHERE [Id] = 4;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727221436_AddProductOverlayField'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [ImagenOverlayUrl] = NULL
    WHERE [Id] = 5;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727221436_AddProductOverlayField'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [ImagenOverlayUrl] = NULL
    WHERE [Id] = 6;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727221436_AddProductOverlayField'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260727221436_AddProductOverlayField', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727221746_UpdateProductOverlayExtensions'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [ImagenOverlayUrl] = N''/img/overlays/rayban_aviator.svg''
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727221746_UpdateProductOverlayExtensions'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [ImagenOverlayUrl] = N''/img/overlays/oakley_sport.svg''
    WHERE [Id] = 3;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727221746_UpdateProductOverlayExtensions'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [ImagenOverlayUrl] = N''/img/overlays/classic.svg''
    WHERE [Id] = 4;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727221746_UpdateProductOverlayExtensions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260727221746_UpdateProductOverlayExtensions', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801230347_AddHistorialExamenFormulaEntities'
)
BEGIN
    CREATE TABLE [ExamenesVisuales] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Fecha] datetime2 NOT NULL,
        [TipoExamen] nvarchar(200) NOT NULL,
        [OjoDerecho] nvarchar(500) NOT NULL,
        [OjoIzquierdo] nvarchar(500) NOT NULL,
        [Resultado] nvarchar(1000) NOT NULL,
        [OptometraId] int NOT NULL,
        [FechaCreacion] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_ExamenesVisuales] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ExamenesVisuales_Users_OptometraId] FOREIGN KEY ([OptometraId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_ExamenesVisuales_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801230347_AddHistorialExamenFormulaEntities'
)
BEGIN
    CREATE TABLE [FormulasOpticas] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Fecha] datetime2 NOT NULL,
        [EsferaOD] nvarchar(50) NOT NULL,
        [CilindroOD] nvarchar(50) NOT NULL,
        [EjeOD] nvarchar(50) NOT NULL,
        [EsferaOI] nvarchar(50) NOT NULL,
        [CilindroOI] nvarchar(50) NOT NULL,
        [EjeOI] nvarchar(50) NOT NULL,
        [Observaciones] nvarchar(1000) NULL,
        [OptometraId] int NOT NULL,
        [FechaCreacion] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_FormulasOpticas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FormulasOpticas_Users_OptometraId] FOREIGN KEY ([OptometraId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_FormulasOpticas_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801230347_AddHistorialExamenFormulaEntities'
)
BEGIN
    CREATE TABLE [HistorialesClinicos] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Fecha] datetime2 NOT NULL,
        [Diagnostico] nvarchar(2000) NOT NULL,
        [Tratamiento] nvarchar(2000) NOT NULL,
        [OptometraId] int NOT NULL,
        [FechaCreacion] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_HistorialesClinicos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HistorialesClinicos_Users_OptometraId] FOREIGN KEY ([OptometraId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_HistorialesClinicos_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801230347_AddHistorialExamenFormulaEntities'
)
BEGIN
    CREATE INDEX [IX_ExamenesVisuales_OptometraId] ON [ExamenesVisuales] ([OptometraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801230347_AddHistorialExamenFormulaEntities'
)
BEGIN
    CREATE INDEX [IX_ExamenesVisuales_UserId] ON [ExamenesVisuales] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801230347_AddHistorialExamenFormulaEntities'
)
BEGIN
    CREATE INDEX [IX_FormulasOpticas_OptometraId] ON [FormulasOpticas] ([OptometraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801230347_AddHistorialExamenFormulaEntities'
)
BEGIN
    CREATE INDEX [IX_FormulasOpticas_UserId] ON [FormulasOpticas] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801230347_AddHistorialExamenFormulaEntities'
)
BEGIN
    CREATE INDEX [IX_HistorialesClinicos_OptometraId] ON [HistorialesClinicos] ([OptometraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801230347_AddHistorialExamenFormulaEntities'
)
BEGIN
    CREATE INDEX [IX_HistorialesClinicos_UserId] ON [HistorialesClinicos] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801230347_AddHistorialExamenFormulaEntities'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260801230347_AddHistorialExamenFormulaEntities', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [Users] ADD [AniosExperiencia] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [Users] ADD [Direccion] nvarchar(255) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [Users] ADD [EPS] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [Users] ADD [EspecialidadDetalle] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [Users] ADD [EstadoPaciente] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [Users] ADD [FechaNacimiento] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [Users] ADD [FotoUrl] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [Users] ADD [Genero] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [Users] ADD [ObservacionesPaciente] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [Users] ADD [RegistroMedico] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [Users] ADD [Universidad] nvarchar(150) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [HistorialesClinicos] ADD [Antecedentes] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [HistorialesClinicos] ADD [Estado] nvarchar(20) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [HistorialesClinicos] ADD [ExamenesRealizados] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [HistorialesClinicos] ADD [Observaciones] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [FormulasOpticas] ADD [DistanciaPupilar] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [FormulasOpticas] ADD [TipoLente] nvarchar(100) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [ExamenesVisuales] ADD [AdicionOD] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [ExamenesVisuales] ADD [AdicionOI] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [ExamenesVisuales] ADD [CilindroOD] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [ExamenesVisuales] ADD [CilindroOI] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [ExamenesVisuales] ADD [Diagnostico] nvarchar(1000) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [ExamenesVisuales] ADD [EjeOD] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [ExamenesVisuales] ADD [EjeOI] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [ExamenesVisuales] ADD [EsferaOD] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [ExamenesVisuales] ADD [EsferaOI] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [ExamenesVisuales] ADD [Observaciones] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [ExamenesVisuales] ADD [SegmentoAnterior] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [ExamenesVisuales] ADD [SegmentoPosterior] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [ExamenesVisuales] ADD [TonometriaOD] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [ExamenesVisuales] ADD [TonometriaOI] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    ALTER TABLE [ExamenesVisuales] ADD [Tratamiento] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    EXEC(N'UPDATE [Users] SET [AniosExperiencia] = NULL, [Direccion] = NULL, [EPS] = NULL, [EspecialidadDetalle] = NULL, [EstadoPaciente] = N''Activo'', [FechaNacimiento] = NULL, [FotoUrl] = NULL, [Genero] = NULL, [ObservacionesPaciente] = NULL, [RegistroMedico] = NULL, [Universidad] = NULL
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    EXEC(N'UPDATE [Users] SET [AniosExperiencia] = NULL, [Direccion] = NULL, [EPS] = NULL, [EspecialidadDetalle] = NULL, [EstadoPaciente] = N''Activo'', [FechaNacimiento] = NULL, [FotoUrl] = NULL, [Genero] = NULL, [ObservacionesPaciente] = NULL, [RegistroMedico] = NULL, [Universidad] = NULL
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    EXEC(N'UPDATE [Users] SET [AniosExperiencia] = NULL, [Direccion] = NULL, [EPS] = NULL, [EspecialidadDetalle] = NULL, [EstadoPaciente] = N''Activo'', [FechaNacimiento] = NULL, [FotoUrl] = NULL, [Genero] = NULL, [ObservacionesPaciente] = NULL, [RegistroMedico] = NULL, [Universidad] = NULL
    WHERE [Id] = 3;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    EXEC(N'UPDATE [Users] SET [AniosExperiencia] = NULL, [Direccion] = NULL, [EPS] = NULL, [EspecialidadDetalle] = NULL, [EstadoPaciente] = N''Activo'', [FechaNacimiento] = NULL, [FotoUrl] = NULL, [Genero] = NULL, [ObservacionesPaciente] = NULL, [RegistroMedico] = NULL, [Universidad] = NULL
    WHERE [Id] = 4;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802014143_CompletarModuloOptometra'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260802014143_CompletarModuloOptometra', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802195701_AddEmployeeRolColumn'
)
BEGIN
    ALTER TABLE [Employees] ADD [Rol] nvarchar(50) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802195701_AddEmployeeRolColumn'
)
BEGIN
    EXEC(N'UPDATE [Employees] SET [Rol] = N''Trabajador''
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802195701_AddEmployeeRolColumn'
)
BEGIN
    EXEC(N'UPDATE [Employees] SET [Rol] = N''Trabajador''
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802195701_AddEmployeeRolColumn'
)
BEGIN
    EXEC(N'UPDATE [Employees] SET [Rol] = N''Trabajador''
    WHERE [Id] = 3;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802195701_AddEmployeeRolColumn'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260802195701_AddEmployeeRolColumn', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803035117_AddSoftDeleteActivoField'
)
BEGIN
    ALTER TABLE [Users] ADD [Activo] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803035117_AddSoftDeleteActivoField'
)
BEGIN
    ALTER TABLE [Orders] ADD [Activo] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803035117_AddSoftDeleteActivoField'
)
BEGIN
    ALTER TABLE [Invoices] ADD [Activo] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803035117_AddSoftDeleteActivoField'
)
BEGIN
    ALTER TABLE [HistorialesClinicos] ADD [Activo] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803035117_AddSoftDeleteActivoField'
)
BEGIN
    ALTER TABLE [FormulasOpticas] ADD [Activo] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803035117_AddSoftDeleteActivoField'
)
BEGIN
    ALTER TABLE [ExamenesVisuales] ADD [Activo] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803035117_AddSoftDeleteActivoField'
)
BEGIN
    ALTER TABLE [Appointments] ADD [Activo] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803035117_AddSoftDeleteActivoField'
)
BEGIN
    EXEC(N'UPDATE [Appointments] SET [Activo] = CAST(1 AS bit)
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803035117_AddSoftDeleteActivoField'
)
BEGIN
    EXEC(N'UPDATE [Appointments] SET [Activo] = CAST(1 AS bit)
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803035117_AddSoftDeleteActivoField'
)
BEGIN
    EXEC(N'UPDATE [Orders] SET [Activo] = CAST(1 AS bit)
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803035117_AddSoftDeleteActivoField'
)
BEGIN
    EXEC(N'UPDATE [Orders] SET [Activo] = CAST(1 AS bit)
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803035117_AddSoftDeleteActivoField'
)
BEGIN
    EXEC(N'UPDATE [Users] SET [Activo] = CAST(1 AS bit)
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803035117_AddSoftDeleteActivoField'
)
BEGIN
    EXEC(N'UPDATE [Users] SET [Activo] = CAST(1 AS bit)
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803035117_AddSoftDeleteActivoField'
)
BEGIN
    EXEC(N'UPDATE [Users] SET [Activo] = CAST(1 AS bit)
    WHERE [Id] = 3;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803035117_AddSoftDeleteActivoField'
)
BEGIN
    EXEC(N'UPDATE [Users] SET [Activo] = CAST(1 AS bit)
    WHERE [Id] = 4;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803035117_AddSoftDeleteActivoField'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260803035117_AddSoftDeleteActivoField', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803182534_AddSuppliersAndInventoryMovements'
)
BEGIN
    CREATE TABLE [InventoryMovements] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [Tipo] nvarchar(20) NOT NULL,
        [Cantidad] int NOT NULL,
        [Fecha] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [Responsable] nvarchar(100) NULL,
        CONSTRAINT [PK_InventoryMovements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InventoryMovements_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803182534_AddSuppliersAndInventoryMovements'
)
BEGIN
    CREATE TABLE [Suppliers] (
        [Id] nvarchar(20) NOT NULL,
        [Nombre] nvarchar(100) NOT NULL,
        [TipoProductos] nvarchar(100) NOT NULL,
        [Telefono] nvarchar(20) NOT NULL,
        [Correo] nvarchar(100) NOT NULL,
        [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [FechaRegistro] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_Suppliers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803182534_AddSuppliersAndInventoryMovements'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Cantidad', N'Fecha', N'ProductId', N'Responsable', N'Tipo') AND [object_id] = OBJECT_ID(N'[InventoryMovements]'))
        SET IDENTITY_INSERT [InventoryMovements] ON;
    EXEC(N'INSERT INTO [InventoryMovements] ([Id], [Cantidad], [Fecha], [ProductId], [Responsable], [Tipo])
    VALUES (1, 20, ''2026-05-01T10:00:00.0000000Z'', 1, N''Administrador'', N''Entrada''),
    (2, 5, ''2026-05-02T14:00:00.0000000Z'', 2, N''Administrador'', N''Salida''),
    (3, 10, ''2026-05-03T11:30:00.0000000Z'', 3, N''Administrador'', N''Entrada'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Cantidad', N'Fecha', N'ProductId', N'Responsable', N'Tipo') AND [object_id] = OBJECT_ID(N'[InventoryMovements]'))
        SET IDENTITY_INSERT [InventoryMovements] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803182534_AddSuppliersAndInventoryMovements'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Correo', N'FechaRegistro', N'Nombre', N'Telefono', N'TipoProductos') AND [object_id] = OBJECT_ID(N'[Suppliers]'))
        SET IDENTITY_INSERT [Suppliers] ON;
    EXEC(N'INSERT INTO [Suppliers] ([Id], [Activo], [Correo], [FechaRegistro], [Nombre], [Telefono], [TipoProductos])
    VALUES (N''PROV001'', CAST(1 AS bit), N''ventas@opticaglobal.com'', ''2026-01-01T00:00:00.0000000Z'', N''Óptica Global S.A.'', N''555-1001'', N''Monturas''),
    (N''PROV002'', CAST(1 AS bit), N''contacto@lenstech.co'', ''2026-01-01T00:00:00.0000000Z'', N''LensTech Colombia'', N''555-1002'', N''Lentes de contacto''),
    (N''PROV003'', CAST(1 AS bit), N''info@distvisual.com'', ''2026-01-01T00:00:00.0000000Z'', N''Distribuidora Visual'', N''555-1003'', N''Accesorios'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Correo', N'FechaRegistro', N'Nombre', N'Telefono', N'TipoProductos') AND [object_id] = OBJECT_ID(N'[Suppliers]'))
        SET IDENTITY_INSERT [Suppliers] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803182534_AddSuppliersAndInventoryMovements'
)
BEGIN
    CREATE INDEX [IX_InventoryMovements_Fecha] ON [InventoryMovements] ([Fecha] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803182534_AddSuppliersAndInventoryMovements'
)
BEGIN
    CREATE INDEX [IX_InventoryMovements_ProductId] ON [InventoryMovements] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803182534_AddSuppliersAndInventoryMovements'
)
BEGIN
    CREATE INDEX [IX_Suppliers_Activo] ON [Suppliers] ([Activo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803182534_AddSuppliersAndInventoryMovements'
)
BEGIN
    CREATE INDEX [IX_Suppliers_Nombre] ON [Suppliers] ([Nombre]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803182534_AddSuppliersAndInventoryMovements'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260803182534_AddSuppliersAndInventoryMovements', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824215616_AddPorcentajeIvaToProduct'
)
BEGIN

    IF NOT EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID(N'[Products]') AND name = 'PorcentajeIva'
    )
    BEGIN
        ALTER TABLE [Products] ADD [PorcentajeIva] decimal(5,2) NOT NULL DEFAULT 19.0;
    END

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824215616_AddPorcentajeIvaToProduct'
)
BEGIN
    EXEC(N'UPDATE [Employees] SET [Salario] = 2500000.0
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824215616_AddPorcentajeIvaToProduct'
)
BEGIN
    EXEC(N'UPDATE [Employees] SET [Salario] = 1800000.0
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824215616_AddPorcentajeIvaToProduct'
)
BEGIN
    EXEC(N'UPDATE [Employees] SET [Email] = N''carlos.mendoza@lentsoft.com'', [Nombre] = N''Carlos Mendoza'', [Salario] = 3500000.0
    WHERE [Id] = 3;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824215616_AddPorcentajeIvaToProduct'
)
BEGIN
    EXEC(N'UPDATE [OrderItems] SET [PrecioUnitario] = 2500000.0
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824215616_AddPorcentajeIvaToProduct'
)
BEGIN
    EXEC(N'UPDATE [OrderItems] SET [PrecioUnitario] = 1800000.0
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824215616_AddPorcentajeIvaToProduct'
)
BEGIN
    EXEC(N'UPDATE [Orders] SET [Total] = 2500000.0
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824215616_AddPorcentajeIvaToProduct'
)
BEGIN
    EXEC(N'UPDATE [Orders] SET [Total] = 1800000.0
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824215616_AddPorcentajeIvaToProduct'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [PorcentajeIva] = 19.0, [Precio] = 2500000.0
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824215616_AddPorcentajeIvaToProduct'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [PorcentajeIva] = 19.0, [Precio] = 450000.0, [PrecioDescuento] = 399000.0
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824215616_AddPorcentajeIvaToProduct'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [PorcentajeIva] = 19.0, [Precio] = 1800000.0
    WHERE [Id] = 3;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824215616_AddPorcentajeIvaToProduct'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [PorcentajeIva] = 19.0, [Precio] = 1200000.0
    WHERE [Id] = 4;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824215616_AddPorcentajeIvaToProduct'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [PorcentajeIva] = 19.0, [Precio] = 150000.0, [PrecioDescuento] = 99000.0
    WHERE [Id] = 5;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824215616_AddPorcentajeIvaToProduct'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [PorcentajeIva] = 5.0, [Precio] = 120000.0
    WHERE [Id] = 6;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824215616_AddPorcentajeIvaToProduct'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260824215616_AddPorcentajeIvaToProduct', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826045418_CitasYOptometraCompleto'
)
BEGIN
    ALTER TABLE [Appointments] ADD [OptometraId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826045418_CitasYOptometraCompleto'
)
BEGIN
    ALTER TABLE [Appointments] ADD [VecesReprogramada] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826045418_CitasYOptometraCompleto'
)
BEGIN
    CREATE TABLE [AuditoriaCitas] (
        [Id] int NOT NULL IDENTITY,
        [AppointmentId] int NOT NULL,
        [EstadoAnterior] nvarchar(20) NOT NULL,
        [EstadoNuevo] nvarchar(20) NOT NULL,
        [FechaCambio] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_AuditoriaCitas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditoriaCitas_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826045418_CitasYOptometraCompleto'
)
BEGIN
    CREATE TABLE [SalesOrders] (
        [Id] int NOT NULL IDENTITY,
        [NumeroPedido] nvarchar(50) NOT NULL,
        [ClienteNombre] nvarchar(150) NOT NULL,
        [ProductoNombre] nvarchar(200) NOT NULL,
        [Cantidad] int NOT NULL,
        [PrecioUnitario] decimal(10,2) NOT NULL,
        [Total] decimal(10,2) NOT NULL,
        [Estado] nvarchar(30) NOT NULL,
        [Fecha] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [Notas] nvarchar(500) NULL,
        [Activo] bit NOT NULL,
        CONSTRAINT [PK_SalesOrders] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826045418_CitasYOptometraCompleto'
)
BEGIN
    CREATE TABLE [SupplierOrders] (
        [Id] int NOT NULL IDENTITY,
        [NumeroPedido] nvarchar(50) NOT NULL,
        [SupplierId] nvarchar(20) NOT NULL,
        [ProductId] int NOT NULL,
        [Cantidad] int NOT NULL,
        [PrecioUnitario] decimal(10,2) NOT NULL,
        [Total] decimal(10,2) NOT NULL,
        [Estado] nvarchar(30) NOT NULL,
        [Fecha] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [Notas] nvarchar(500) NULL,
        [Activo] bit NOT NULL,
        CONSTRAINT [PK_SupplierOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SupplierOrders_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SupplierOrders_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826045418_CitasYOptometraCompleto'
)
BEGIN
    EXEC(N'UPDATE [Appointments] SET [OptometraId] = 3
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826045418_CitasYOptometraCompleto'
)
BEGIN
    EXEC(N'UPDATE [Appointments] SET [OptometraId] = 3
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826045418_CitasYOptometraCompleto'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Cantidad', N'ClienteNombre', N'Estado', N'Fecha', N'Notas', N'NumeroPedido', N'PrecioUnitario', N'ProductoNombre', N'Total') AND [object_id] = OBJECT_ID(N'[SalesOrders]'))
        SET IDENTITY_INSERT [SalesOrders] ON;
    EXEC(N'INSERT INTO [SalesOrders] ([Id], [Activo], [Cantidad], [ClienteNombre], [Estado], [Fecha], [Notas], [NumeroPedido], [PrecioUnitario], [ProductoNombre], [Total])
    VALUES (1, CAST(1 AS bit), 2, N''Carlos Ramírez'', N''entregado'', ''2026-05-12T00:00:00.0000000Z'', NULL, N''PED-VENTA-001'', 350000.0, N''Gafas de Sol Polarizadas Especiales'', 700000.0),
    (2, CAST(1 AS bit), 1, N''Ana María Torres'', N''enviado'', ''2026-05-22T00:00:00.0000000Z'', NULL, N''PED-VENTA-002'', 480000.0, N''Lentes de Contacto Toricos Custom'', 480000.0)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Cantidad', N'ClienteNombre', N'Estado', N'Fecha', N'Notas', N'NumeroPedido', N'PrecioUnitario', N'ProductoNombre', N'Total') AND [object_id] = OBJECT_ID(N'[SalesOrders]'))
        SET IDENTITY_INSERT [SalesOrders] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826045418_CitasYOptometraCompleto'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Cantidad', N'Estado', N'Fecha', N'Notas', N'NumeroPedido', N'PrecioUnitario', N'ProductId', N'SupplierId', N'Total') AND [object_id] = OBJECT_ID(N'[SupplierOrders]'))
        SET IDENTITY_INSERT [SupplierOrders] ON;
    EXEC(N'INSERT INTO [SupplierOrders] ([Id], [Activo], [Cantidad], [Estado], [Fecha], [Notas], [NumeroPedido], [PrecioUnitario], [ProductId], [SupplierId], [Total])
    VALUES (1, CAST(1 AS bit), 15, N''recibido'', ''2026-05-10T00:00:00.0000000Z'', NULL, N''PED-PROV-001'', 1200000.0, 1, N''PROV001'', 18000000.0),
    (2, CAST(1 AS bit), 30, N''pendiente'', ''2026-05-20T00:00:00.0000000Z'', NULL, N''PED-PROV-002'', 200000.0, 2, N''PROV002'', 6000000.0)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Cantidad', N'Estado', N'Fecha', N'Notas', N'NumeroPedido', N'PrecioUnitario', N'ProductId', N'SupplierId', N'Total') AND [object_id] = OBJECT_ID(N'[SupplierOrders]'))
        SET IDENTITY_INSERT [SupplierOrders] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826045418_CitasYOptometraCompleto'
)
BEGIN
    CREATE INDEX [IX_Appointments_OptometraId] ON [Appointments] ([OptometraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826045418_CitasYOptometraCompleto'
)
BEGIN
    CREATE INDEX [IX_AuditoriaCitas_AppointmentId] ON [AuditoriaCitas] ([AppointmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826045418_CitasYOptometraCompleto'
)
BEGIN
    CREATE INDEX [IX_AuditoriaCitas_FechaCambio] ON [AuditoriaCitas] ([FechaCambio] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826045418_CitasYOptometraCompleto'
)
BEGIN
    CREATE INDEX [IX_SalesOrders_NumeroPedido] ON [SalesOrders] ([NumeroPedido]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826045418_CitasYOptometraCompleto'
)
BEGIN
    CREATE INDEX [IX_SupplierOrders_NumeroPedido] ON [SupplierOrders] ([NumeroPedido]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826045418_CitasYOptometraCompleto'
)
BEGIN
    CREATE INDEX [IX_SupplierOrders_ProductId] ON [SupplierOrders] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826045418_CitasYOptometraCompleto'
)
BEGIN
    CREATE INDEX [IX_SupplierOrders_SupplierId] ON [SupplierOrders] ([SupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826045418_CitasYOptometraCompleto'
)
BEGIN
    ALTER TABLE [Appointments] ADD CONSTRAINT [FK_Appointments_Users_OptometraId] FOREIGN KEY ([OptometraId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826045418_CitasYOptometraCompleto'
)
BEGIN

    CREATE OR ALTER PROCEDURE sp_VerificarDisponibilidadCita
        @OptometraId     INT,
        @FechaHora       DATETIME,
        @DuracionMinutos INT = 60,
        @ExcluirCitaId   INT = NULL
    AS
    BEGIN
        SET NOCOUNT ON;
        DECLARE @FinNueva DATETIME = DATEADD(MINUTE, @DuracionMinutos, @FechaHora);

        IF EXISTS (
            SELECT 1
            FROM   Appointments
            WHERE  Activo = 1
              AND  OptometraId = @OptometraId
              AND  LOWER(Estado) <> 'cancelada'
              AND  (@ExcluirCitaId IS NULL OR Id <> @ExcluirCitaId)
              AND  FechaHora < @FinNueva
              AND  DATEADD(MINUTE, @DuracionMinutos, FechaHora) > @FechaHora
        )
            SELECT 0 AS Disponible;
        ELSE
            SELECT 1 AS Disponible;
    END;

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826045418_CitasYOptometraCompleto'
)
BEGIN

    CREATE OR ALTER PROCEDURE sp_HistorialCompletoPaciente
        @PacienteId INT
    AS
    BEGIN
        SET NOCOUNT ON;

        SELECT 'Historial'                AS TipoRegistro,
               hc.Fecha                  AS Fecha,
               ISNULL(hc.Diagnostico,'') AS Descripcion,
               hc.Observaciones          AS Detalles
        FROM   HistorialesClinicos hc
        WHERE  hc.UserId = @PacienteId AND hc.Activo = 1

        UNION ALL

        SELECT 'Examen'                   AS TipoRegistro,
               ev.Fecha                   AS Fecha,
               ISNULL(ev.TipoExamen,'')   AS Descripcion,
               ev.Resultado               AS Detalles
        FROM   ExamenesVisuales ev
        WHERE  ev.UserId = @PacienteId AND ev.Activo = 1

        UNION ALL

        SELECT 'Formula'                  AS TipoRegistro,
               fo.Fecha                   AS Fecha,
               ISNULL(fo.TipoLente,'')    AS Descripcion,
               fo.Observaciones           AS Detalles
        FROM   FormulasOpticas fo
        WHERE  fo.UserId = @PacienteId AND fo.Activo = 1

        ORDER BY Fecha DESC;
    END;

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826045418_CitasYOptometraCompleto'
)
BEGIN

    CREATE OR ALTER PROCEDURE sp_ReporteCitasPorEstado
        @FechaInicio DATETIME,
        @FechaFin    DATETIME,
        @OptometraId INT = NULL
    AS
    BEGIN
        SET NOCOUNT ON;

        SELECT Estado,
               COUNT(*) AS Total
        FROM   Appointments
        WHERE  Activo = 1
          AND  FechaHora >= @FechaInicio
          AND  FechaHora <= @FechaFin
          AND  (@OptometraId IS NULL OR OptometraId = @OptometraId)
        GROUP BY Estado
        ORDER BY Total DESC;
    END;

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826045418_CitasYOptometraCompleto'
)
BEGIN

    CREATE OR ALTER PROCEDURE sp_RegistrarCitaConValidacion
        @UserId      INT,
        @OptometraId INT,
        @Servicio    NVARCHAR(100),
        @FechaHora   DATETIME,
        @Notas       NVARCHAR(500) = NULL,
        @NuevaCitaId INT OUTPUT
    AS
    BEGIN
        SET NOCOUNT ON;
        BEGIN TRANSACTION;

        -- Validar horario laboral. DATEPART(WEEKDAY): 1=Domingo (independiente de idioma/collation).
        DECLARE @DiaSemana INT = DATEPART(WEEKDAY, @FechaHora);
        DECLARE @Hora      INT = DATEPART(HOUR,    @FechaHora);

        IF @DiaSemana = 1 OR @Hora < 8 OR @Hora >= 18
        BEGIN
            ROLLBACK;
            THROW 50001, 'No se puede agendar: las citas solo se permiten de lunes a sabado, entre 8:00 a.m. y 6:00 p.m.', 1;
            RETURN;
        END

        -- Verificar disponibilidad del optometra
        DECLARE @FinNueva DATETIME = DATEADD(MINUTE, 60, @FechaHora);
        IF EXISTS (
            SELECT 1
            FROM   Appointments
            WHERE  Activo = 1
              AND  OptometraId = @OptometraId
              AND  LOWER(Estado) <> 'cancelada'
              AND  FechaHora < @FinNueva
              AND  DATEADD(MINUTE, 60, FechaHora) > @FechaHora
        )
        BEGIN
            ROLLBACK;
            THROW 50002, 'No se puede agendar: el optometra ya tiene una cita en ese horario.', 1;
            RETURN;
        END

        -- Insertar la cita validada
        INSERT INTO Appointments
            (UserId, OptometraId, Servicio, FechaHora, Estado, Notas, FechaCreacion, Activo, VecesReprogramada)
        VALUES
            (@UserId, @OptometraId, @Servicio, @FechaHora, 'pendiente', @Notas, GETUTCDATE(), 1, 0);

        SET @NuevaCitaId = SCOPE_IDENTITY();
        COMMIT;
    END;

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826045418_CitasYOptometraCompleto'
)
BEGIN

    CREATE OR ALTER TRIGGER trg_Appointment_PreventOverlap
    ON  Appointments
    AFTER INSERT, UPDATE
    AS
    BEGIN
        SET NOCOUNT ON;

        -- Omitir filas inactivas o canceladas sin OptometraId
        IF NOT EXISTS (
            SELECT 1 FROM inserted
            WHERE  Activo = 1 AND LOWER(Estado) <> 'cancelada' AND OptometraId IS NOT NULL
        ) RETURN;

        -- a) Validar horario laboral (DATEPART(WEEKDAY): 1=Domingo, independiente de idioma)
        IF EXISTS (
            SELECT 1 FROM inserted
            WHERE  Activo = 1
              AND  LOWER(Estado) <> 'cancelada'
              AND  (   DATEPART(WEEKDAY, FechaHora) = 1
                   OR  DATEPART(HOUR, FechaHora) < 8
                   OR  DATEPART(HOUR, FechaHora) >= 18)
        )
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 50001, 'No se puede agendar: las citas solo se permiten de lunes a sabado, entre 8:00 a.m. y 6:00 p.m.', 1;
        END

        -- b) Validar solapamiento por optometra (solo si pasÃ³ la validaciÃ³n de horario)
        IF EXISTS (
            SELECT 1
            FROM   inserted  i
            JOIN   Appointments a
                   ON  a.OptometraId = i.OptometraId
                   AND a.Activo      = 1
                   AND LOWER(a.Estado) <> 'cancelada'
                   AND a.Id          <> i.Id
            WHERE  i.Activo  = 1
              AND  LOWER(i.Estado) <> 'cancelada'
              AND  i.OptometraId IS NOT NULL
              AND  i.FechaHora < DATEADD(MINUTE, 60, a.FechaHora)
              AND  DATEADD(MINUTE, 60, i.FechaHora) > a.FechaHora
        )
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 50002, 'No se puede agendar: el optometra ya tiene una cita en ese horario.', 1;
        END
    END;

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826045418_CitasYOptometraCompleto'
)
BEGIN

    CREATE OR ALTER TRIGGER trg_Appointment_Auditoria
    ON  Appointments
    AFTER UPDATE
    AS
    BEGIN
        SET NOCOUNT ON;

        INSERT INTO AuditoriaCitas (AppointmentId, EstadoAnterior, EstadoNuevo, FechaCambio)
        SELECT i.Id, d.Estado, i.Estado, GETUTCDATE()
        FROM   inserted i
        JOIN   deleted  d ON d.Id = i.Id
        WHERE  i.Estado <> d.Estado;
    END;

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826045418_CitasYOptometraCompleto'
)
BEGIN

    CREATE OR ALTER TRIGGER trg_HistorialClinico_PrevenirEliminacionConFormula
    ON  HistorialesClinicos
    INSTEAD OF DELETE
    AS
    BEGIN
        SET NOCOUNT ON;

        IF EXISTS (
            SELECT 1
            FROM   deleted d
            JOIN   FormulasOpticas fo ON fo.UserId = d.UserId AND fo.Activo = 1
        )
        BEGIN
            THROW 50003, 'No se puede eliminar el historial: tiene una formula optica asociada.', 1;
            RETURN;
        END

        DELETE hc
        FROM   HistorialesClinicos hc
        JOIN   deleted d ON d.Id = hc.Id;
    END;

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826045418_CitasYOptometraCompleto'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260826045418_CitasYOptometraCompleto', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902213000_UpdateInventoryAndSupplierSchema'
)
BEGIN
    ALTER TABLE [InventoryMovements] ADD [NombreProducto] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902213000_UpdateInventoryAndSupplierSchema'
)
BEGIN
    ALTER TABLE [Suppliers] ADD [Contacto] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902213000_UpdateInventoryAndSupplierSchema'
)
BEGIN
    ALTER TABLE [Suppliers] ADD [LogoUrl] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902213000_UpdateInventoryAndSupplierSchema'
)
BEGIN
    ALTER TABLE [Products] ADD [SupplierId] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902213000_UpdateInventoryAndSupplierSchema'
)
BEGIN
    CREATE INDEX [IX_Products_SupplierId] ON [Products] ([SupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902213000_UpdateInventoryAndSupplierSchema'
)
BEGIN
    ALTER TABLE [Products] ADD CONSTRAINT [FK_Products_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902213000_UpdateInventoryAndSupplierSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260902213000_UpdateInventoryAndSupplierSchema', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904220503_AddSupplierIdToProduct'
)
BEGIN

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Suppliers') AND name = 'Contacto')
        ALTER TABLE [Suppliers] ADD [Contacto] nvarchar(100) NULL;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Suppliers') AND name = 'Email')
        ALTER TABLE [Suppliers] ADD [Email] nvarchar(max) NOT NULL DEFAULT '';
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Suppliers') AND name = 'Estado')
        ALTER TABLE [Suppliers] ADD [Estado] nvarchar(max) NOT NULL DEFAULT '';
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Suppliers') AND name = 'LogoUrl')
        ALTER TABLE [Suppliers] ADD [LogoUrl] nvarchar(500) NULL;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Suppliers') AND name = 'TipoProducto')
        ALTER TABLE [Suppliers] ADD [TipoProducto] nvarchar(max) NOT NULL DEFAULT '';
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'SupplierId')
        ALTER TABLE [Products] ADD [SupplierId] nvarchar(20) NULL;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('InventoryMovements') AND name = 'NombreProducto')
        ALTER TABLE [InventoryMovements] ADD [NombreProducto] nvarchar(200) NULL;

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904220503_AddSupplierIdToProduct'
)
BEGIN
    EXEC(N'UPDATE [InventoryMovements] SET [NombreProducto] = N''Lentes Ray-Ban Aviator''
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904220503_AddSupplierIdToProduct'
)
BEGIN
    EXEC(N'UPDATE [InventoryMovements] SET [NombreProducto] = N''Lentes de Contacto Acuvue''
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904220503_AddSupplierIdToProduct'
)
BEGIN
    EXEC(N'UPDATE [InventoryMovements] SET [NombreProducto] = N''Montura Oakley Sport''
    WHERE [Id] = 3;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904220503_AddSupplierIdToProduct'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [SupplierId] = NULL
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904220503_AddSupplierIdToProduct'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [SupplierId] = NULL
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904220503_AddSupplierIdToProduct'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [SupplierId] = NULL
    WHERE [Id] = 3;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904220503_AddSupplierIdToProduct'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [SupplierId] = NULL
    WHERE [Id] = 4;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904220503_AddSupplierIdToProduct'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [SupplierId] = NULL
    WHERE [Id] = 5;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904220503_AddSupplierIdToProduct'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [SupplierId] = NULL
    WHERE [Id] = 6;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904220503_AddSupplierIdToProduct'
)
BEGIN
    EXEC(N'UPDATE [Suppliers] SET [Contacto] = N''Carlos Gómez'', [Email] = N''ventas@opticaglobal.com'', [Estado] = N''Activo'', [LogoUrl] = N''https://images.unsplash.com/photo-1560179707-f14e90ef3623'', [TipoProducto] = N''Monturas''
    WHERE [Id] = N''PROV001'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904220503_AddSupplierIdToProduct'
)
BEGIN
    EXEC(N'UPDATE [Suppliers] SET [Contacto] = N''Ana Martínez'', [Email] = N''contacto@lenstech.co'', [Estado] = N''Activo'', [LogoUrl] = N''https://images.unsplash.com/photo-1572021335469-31706a17aaef'', [TipoProducto] = N''Lentes de contacto''
    WHERE [Id] = N''PROV002'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904220503_AddSupplierIdToProduct'
)
BEGIN
    EXEC(N'UPDATE [Suppliers] SET [Contacto] = N''Roberto Díaz'', [Email] = N''info@distvisual.com'', [Estado] = N''Activo'', [LogoUrl] = N''https://images.unsplash.com/photo-1556761175-5973dc0f32e7'', [TipoProducto] = N''Accesorios''
    WHERE [Id] = N''PROV003'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904220503_AddSupplierIdToProduct'
)
BEGIN

    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_SupplierId' AND object_id = OBJECT_ID('Products'))
        CREATE INDEX [IX_Products_SupplierId] ON [Products] ([SupplierId]);

    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Products_Suppliers_SupplierId')
        ALTER TABLE [Products] ADD CONSTRAINT [FK_Products_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]);

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904220503_AddSupplierIdToProduct'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260904220503_AddSupplierIdToProduct', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914205527_UpdateProductOverlaysPng'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [ImagenOverlayUrl] = N''/img/overlays/rayban_aviator_final.png''
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914205527_UpdateProductOverlaysPng'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [ImagenOverlayUrl] = N''/img/overlays/lentes_graduados_classic_final.png''
    WHERE [Id] = 4;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914205527_UpdateProductOverlaysPng'
)
BEGIN
    UPDATE Products SET ImagenOverlayUrl = '/img/overlays/obrien_deluxe_final.png' WHERE Nombre LIKE '%O''Brien%';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914205527_UpdateProductOverlaysPng'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260914205527_UpdateProductOverlaysPng', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260923014631_AddDatosRefraccionAHistorialClinico'
)
BEGIN
    ALTER TABLE [HistorialesClinicos] ADD [CilindroOD] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260923014631_AddDatosRefraccionAHistorialClinico'
)
BEGIN
    ALTER TABLE [HistorialesClinicos] ADD [CilindroOI] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260923014631_AddDatosRefraccionAHistorialClinico'
)
BEGIN
    ALTER TABLE [HistorialesClinicos] ADD [EjeOD] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260923014631_AddDatosRefraccionAHistorialClinico'
)
BEGIN
    ALTER TABLE [HistorialesClinicos] ADD [EjeOI] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260923014631_AddDatosRefraccionAHistorialClinico'
)
BEGIN
    ALTER TABLE [HistorialesClinicos] ADD [EsferaOD] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260923014631_AddDatosRefraccionAHistorialClinico'
)
BEGIN
    ALTER TABLE [HistorialesClinicos] ADD [EsferaOI] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260923014631_AddDatosRefraccionAHistorialClinico'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260923014631_AddDatosRefraccionAHistorialClinico', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924035109_AddAbonosTransaccionesAndDianFields'
)
BEGIN
    ALTER TABLE [Orders] ADD [EstadoPago] nvarchar(20) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924035109_AddAbonosTransaccionesAndDianFields'
)
BEGIN
    ALTER TABLE [Orders] ADD [MontoPagado] decimal(10,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924035109_AddAbonosTransaccionesAndDianFields'
)
BEGIN
    ALTER TABLE [Invoices] ADD [CUFE] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924035109_AddAbonosTransaccionesAndDianFields'
)
BEGIN
    ALTER TABLE [Invoices] ADD [RangoAutorizadoDesde] nvarchar(50) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924035109_AddAbonosTransaccionesAndDianFields'
)
BEGIN
    ALTER TABLE [Invoices] ADD [RangoAutorizadoHasta] nvarchar(50) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924035109_AddAbonosTransaccionesAndDianFields'
)
BEGIN
    ALTER TABLE [Invoices] ADD [ResolucionDianNumero] nvarchar(50) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924035109_AddAbonosTransaccionesAndDianFields'
)
BEGIN
    CREATE TABLE [PagosVentas] (
        [Id] int NOT NULL IDENTITY,
        [VentaId] int NOT NULL,
        [Monto] decimal(10,2) NOT NULL,
        [FechaPago] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [MetodoPago] nvarchar(50) NOT NULL,
        [Responsable] nvarchar(100) NOT NULL,
        [NumeroComprobante] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_PagosVentas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PagosVentas_Orders_VentaId] FOREIGN KEY ([VentaId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924035109_AddAbonosTransaccionesAndDianFields'
)
BEGIN
    CREATE TABLE [TransaccionesPagos] (
        [Id] int NOT NULL IDENTITY,
        [VentaId] int NULL,
        [Monto] decimal(10,2) NOT NULL,
        [Estado] nvarchar(30) NOT NULL,
        [CodigoAutorizacion] nvarchar(50) NULL,
        [UltimosDigitosTarjeta] nvarchar(10) NULL,
        [MarcaTarjeta] nvarchar(50) NULL,
        [MensajeRespuesta] nvarchar(250) NULL,
        [Fecha] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_TransaccionesPagos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TransaccionesPagos_Orders_VentaId] FOREIGN KEY ([VentaId]) REFERENCES [Orders] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924035109_AddAbonosTransaccionesAndDianFields'
)
BEGIN
    EXEC(N'UPDATE [Orders] SET [EstadoPago] = N''pendiente'', [MontoPagado] = 0.0
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924035109_AddAbonosTransaccionesAndDianFields'
)
BEGIN
    EXEC(N'UPDATE [Orders] SET [EstadoPago] = N''pendiente'', [MontoPagado] = 0.0
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924035109_AddAbonosTransaccionesAndDianFields'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PagosVentas_NumeroComprobante] ON [PagosVentas] ([NumeroComprobante]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924035109_AddAbonosTransaccionesAndDianFields'
)
BEGIN
    CREATE INDEX [IX_PagosVentas_VentaId] ON [PagosVentas] ([VentaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924035109_AddAbonosTransaccionesAndDianFields'
)
BEGIN
    CREATE INDEX [IX_TransaccionesPagos_Fecha] ON [TransaccionesPagos] ([Fecha] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924035109_AddAbonosTransaccionesAndDianFields'
)
BEGIN
    CREATE INDEX [IX_TransaccionesPagos_VentaId] ON [TransaccionesPagos] ([VentaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924035109_AddAbonosTransaccionesAndDianFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260924035109_AddAbonosTransaccionesAndDianFields', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924041156_IncreaseDecimalPrecisionTo18_2'
)
BEGIN
    DECLARE @var sysname;
    SELECT @var = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TransaccionesPagos]') AND [c].[name] = N'Monto');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [TransaccionesPagos] DROP CONSTRAINT [' + @var + '];');
    ALTER TABLE [TransaccionesPagos] ALTER COLUMN [Monto] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924041156_IncreaseDecimalPrecisionTo18_2'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PagosVentas]') AND [c].[name] = N'Monto');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [PagosVentas] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [PagosVentas] ALTER COLUMN [Monto] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924041156_IncreaseDecimalPrecisionTo18_2'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Orders]') AND [c].[name] = N'Total');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Orders] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [Orders] ALTER COLUMN [Total] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924041156_IncreaseDecimalPrecisionTo18_2'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Orders]') AND [c].[name] = N'MontoPagado');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Orders] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [Orders] ALTER COLUMN [MontoPagado] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924041156_IncreaseDecimalPrecisionTo18_2'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoices]') AND [c].[name] = N'Total');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Invoices] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [Invoices] ALTER COLUMN [Total] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924041156_IncreaseDecimalPrecisionTo18_2'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoices]') AND [c].[name] = N'Subtotal');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Invoices] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [Invoices] ALTER COLUMN [Subtotal] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924041156_IncreaseDecimalPrecisionTo18_2'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoices]') AND [c].[name] = N'Impuestos');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Invoices] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [Invoices] ALTER COLUMN [Impuestos] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924041156_IncreaseDecimalPrecisionTo18_2'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260924041156_IncreaseDecimalPrecisionTo18_2', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007214500_AddFormulaOpticaToOrder'
)
BEGIN
    ALTER TABLE [Orders] ADD [FormulaOpticaId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007214500_AddFormulaOpticaToOrder'
)
BEGIN
    CREATE INDEX [IX_Orders_FormulaOpticaId] ON [Orders] ([FormulaOpticaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007214500_AddFormulaOpticaToOrder'
)
BEGIN
    ALTER TABLE [Orders] ADD CONSTRAINT [FK_Orders_FormulasOpticas_FormulaOpticaId] FOREIGN KEY ([FormulaOpticaId]) REFERENCES [FormulasOpticas] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007214500_AddFormulaOpticaToOrder'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261007214500_AddFormulaOpticaToOrder', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008175514_AddOverlayAdjustmentFieldsToProduct'
)
BEGIN
    ALTER TABLE [Products] ADD [EscalaOverlay] decimal(5,2) NOT NULL DEFAULT 2.3;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008175514_AddOverlayAdjustmentFieldsToProduct'
)
BEGIN
    ALTER TABLE [Products] ADD [OffsetXOverlay] decimal(5,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008175514_AddOverlayAdjustmentFieldsToProduct'
)
BEGIN
    ALTER TABLE [Products] ADD [OffsetYOverlay] decimal(5,2) NOT NULL DEFAULT 0.12;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008175514_AddOverlayAdjustmentFieldsToProduct'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [EscalaOverlay] = 2.3, [OffsetXOverlay] = 0.0, [OffsetYOverlay] = 0.12
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008175514_AddOverlayAdjustmentFieldsToProduct'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [EscalaOverlay] = 2.3, [OffsetXOverlay] = 0.0, [OffsetYOverlay] = 0.12
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008175514_AddOverlayAdjustmentFieldsToProduct'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [EscalaOverlay] = 2.3, [OffsetXOverlay] = 0.0, [OffsetYOverlay] = 0.12
    WHERE [Id] = 3;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008175514_AddOverlayAdjustmentFieldsToProduct'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [EscalaOverlay] = 2.3, [OffsetXOverlay] = 0.0, [OffsetYOverlay] = 0.12
    WHERE [Id] = 4;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008175514_AddOverlayAdjustmentFieldsToProduct'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [EscalaOverlay] = 2.3, [OffsetXOverlay] = 0.0, [OffsetYOverlay] = 0.12
    WHERE [Id] = 5;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008175514_AddOverlayAdjustmentFieldsToProduct'
)
BEGIN
    EXEC(N'UPDATE [Products] SET [EscalaOverlay] = 2.3, [OffsetXOverlay] = 0.0, [OffsetYOverlay] = 0.12
    WHERE [Id] = 6;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008175514_AddOverlayAdjustmentFieldsToProduct'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008175514_AddOverlayAdjustmentFieldsToProduct', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009111911_AddProductRowVersionConcurrency'
)
BEGIN
    ALTER TABLE [Products] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009111911_AddProductRowVersionConcurrency'
)
BEGIN
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Orders]') AND [c].[name] = N'Total');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Orders] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [Orders] ALTER COLUMN [Total] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009111911_AddProductRowVersionConcurrency'
)
BEGIN
    ALTER TABLE [Orders] ADD [EstadoPago] nvarchar(20) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009111911_AddProductRowVersionConcurrency'
)
BEGIN
    ALTER TABLE [Orders] ADD [MontoPagado] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009111911_AddProductRowVersionConcurrency'
)
BEGIN
    DECLARE @var8 sysname;
    SELECT @var8 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoices]') AND [c].[name] = N'Total');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Invoices] DROP CONSTRAINT [' + @var8 + '];');
    ALTER TABLE [Invoices] ALTER COLUMN [Total] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009111911_AddProductRowVersionConcurrency'
)
BEGIN
    DECLARE @var9 sysname;
    SELECT @var9 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoices]') AND [c].[name] = N'Subtotal');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Invoices] DROP CONSTRAINT [' + @var9 + '];');
    ALTER TABLE [Invoices] ALTER COLUMN [Subtotal] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009111911_AddProductRowVersionConcurrency'
)
BEGIN
    DECLARE @var10 sysname;
    SELECT @var10 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoices]') AND [c].[name] = N'Impuestos');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Invoices] DROP CONSTRAINT [' + @var10 + '];');
    ALTER TABLE [Invoices] ALTER COLUMN [Impuestos] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009111911_AddProductRowVersionConcurrency'
)
BEGIN
    ALTER TABLE [Invoices] ADD [CUFE] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009111911_AddProductRowVersionConcurrency'
)
BEGIN
    ALTER TABLE [Invoices] ADD [RangoAutorizadoDesde] nvarchar(50) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009111911_AddProductRowVersionConcurrency'
)
BEGIN
    ALTER TABLE [Invoices] ADD [RangoAutorizadoHasta] nvarchar(50) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009111911_AddProductRowVersionConcurrency'
)
BEGIN
    ALTER TABLE [Invoices] ADD [ResolucionDianNumero] nvarchar(50) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009111911_AddProductRowVersionConcurrency'
)
BEGIN
    ALTER TABLE [HistorialesClinicos] ADD [CilindroOD] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009111911_AddProductRowVersionConcurrency'
)
BEGIN
    ALTER TABLE [HistorialesClinicos] ADD [CilindroOI] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009111911_AddProductRowVersionConcurrency'
)
BEGIN
    ALTER TABLE [HistorialesClinicos] ADD [EjeOD] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009111911_AddProductRowVersionConcurrency'
)
BEGIN
    ALTER TABLE [HistorialesClinicos] ADD [EjeOI] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009111911_AddProductRowVersionConcurrency'
)
BEGIN
    ALTER TABLE [HistorialesClinicos] ADD [EsferaOD] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009111911_AddProductRowVersionConcurrency'
)
BEGIN
    ALTER TABLE [HistorialesClinicos] ADD [EsferaOI] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009111911_AddProductRowVersionConcurrency'
)
BEGIN
    CREATE TABLE [PagosVentas] (
        [Id] int NOT NULL IDENTITY,
        [VentaId] int NOT NULL,
        [Monto] decimal(18,2) NOT NULL,
        [FechaPago] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [MetodoPago] nvarchar(50) NOT NULL,
        [Responsable] nvarchar(100) NOT NULL,
        [NumeroComprobante] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_PagosVentas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PagosVentas_Orders_VentaId] FOREIGN KEY ([VentaId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009111911_AddProductRowVersionConcurrency'
)
BEGIN
    CREATE TABLE [TransaccionesPagos] (
        [Id] int NOT NULL IDENTITY,
        [VentaId] int NULL,
        [Monto] decimal(18,2) NOT NULL,
        [Estado] nvarchar(30) NOT NULL,
        [CodigoAutorizacion] nvarchar(50) NULL,
        [UltimosDigitosTarjeta] nvarchar(10) NULL,
        [MarcaTarjeta] nvarchar(50) NULL,
        [MensajeRespuesta] nvarchar(250) NULL,
        [Fecha] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_TransaccionesPagos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TransaccionesPagos_Orders_VentaId] FOREIGN KEY ([VentaId]) REFERENCES [Orders] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009111911_AddProductRowVersionConcurrency'
)
BEGIN
    EXEC(N'UPDATE [Orders] SET [EstadoPago] = N''pendiente'', [MontoPagado] = 0.0
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009111911_AddProductRowVersionConcurrency'
)
BEGIN
    EXEC(N'UPDATE [Orders] SET [EstadoPago] = N''pendiente'', [MontoPagado] = 0.0
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009111911_AddProductRowVersionConcurrency'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PagosVentas_NumeroComprobante] ON [PagosVentas] ([NumeroComprobante]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009111911_AddProductRowVersionConcurrency'
)
BEGIN
    CREATE INDEX [IX_PagosVentas_VentaId] ON [PagosVentas] ([VentaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009111911_AddProductRowVersionConcurrency'
)
BEGIN
    CREATE INDEX [IX_TransaccionesPagos_Fecha] ON [TransaccionesPagos] ([Fecha] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009111911_AddProductRowVersionConcurrency'
)
BEGIN
    CREATE INDEX [IX_TransaccionesPagos_VentaId] ON [TransaccionesPagos] ([VentaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009111911_AddProductRowVersionConcurrency'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261009111911_AddProductRowVersionConcurrency', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013183713_AddInventoryProductFields'
)
BEGIN
    ALTER TABLE [Products] ADD [Sku] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013183713_AddInventoryProductFields'
)
BEGIN
    ALTER TABLE [Products] ADD [CostoCompra] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013183713_AddInventoryProductFields'
)
BEGIN
    ALTER TABLE [Products] ADD [StockMinimo] int NOT NULL DEFAULT 10;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013183713_AddInventoryProductFields'
)
BEGIN
    ALTER TABLE [Products] ADD [Ubicacion] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013183713_AddInventoryProductFields'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Products_Sku] ON [Products] ([Sku]) WHERE [Sku] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013183713_AddInventoryProductFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013183713_AddInventoryProductFields', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013191924_AddInventoryMovementFields'
)
BEGIN
    ALTER TABLE [InventoryMovements] ADD [Motivo] TEXT NOT NULL DEFAULT 'Otro';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013191924_AddInventoryMovementFields'
)
BEGIN
    ALTER TABLE [InventoryMovements] ADD [Nota] TEXT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013191924_AddInventoryMovementFields'
)
BEGIN
    ALTER TABLE [InventoryMovements] ADD [Referencia] TEXT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013191924_AddInventoryMovementFields'
)
BEGIN
    ALTER TABLE [InventoryMovements] ADD [StockResultante] INTEGER NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013191924_AddInventoryMovementFields'
)
BEGIN
    EXEC(N'UPDATE [InventoryMovements] SET [Motivo] = ''Otro'', [Nota] = NULL, [Referencia] = NULL
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013191924_AddInventoryMovementFields'
)
BEGIN
    EXEC(N'UPDATE [InventoryMovements] SET [Motivo] = ''Otro'', [Nota] = NULL, [Referencia] = NULL
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013191924_AddInventoryMovementFields'
)
BEGIN
    EXEC(N'UPDATE [InventoryMovements] SET [Motivo] = ''Otro'', [Nota] = NULL, [Referencia] = NULL
    WHERE [Id] = 3;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013191924_AddInventoryMovementFields'
)
BEGIN
    CREATE INDEX [IX_InventoryMovements_Tipo] ON [InventoryMovements] ([Tipo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013191924_AddInventoryMovementFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013191924_AddInventoryMovementFields', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013193459_AddSupplierAndOrderFields'
)
BEGIN
    ALTER TABLE [Suppliers] ADD [Direccion] TEXT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013193459_AddSupplierAndOrderFields'
)
BEGIN
    ALTER TABLE [Suppliers] ADD [TiempoEntregaDias] INTEGER NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013193459_AddSupplierAndOrderFields'
)
BEGIN
    ALTER TABLE [SupplierOrders] ADD [CantidadRecibida] INTEGER NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013193459_AddSupplierAndOrderFields'
)
BEGIN
    EXEC(N'UPDATE [SupplierOrders] SET [CantidadRecibida] = 0
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013193459_AddSupplierAndOrderFields'
)
BEGIN
    EXEC(N'UPDATE [SupplierOrders] SET [CantidadRecibida] = 0
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013193459_AddSupplierAndOrderFields'
)
BEGIN
    EXEC(N'UPDATE [Suppliers] SET [Direccion] = NULL, [TiempoEntregaDias] = NULL
    WHERE [Id] = ''PROV001'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013193459_AddSupplierAndOrderFields'
)
BEGIN
    EXEC(N'UPDATE [Suppliers] SET [Direccion] = NULL, [TiempoEntregaDias] = NULL
    WHERE [Id] = ''PROV002'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013193459_AddSupplierAndOrderFields'
)
BEGIN
    EXEC(N'UPDATE [Suppliers] SET [Direccion] = NULL, [TiempoEntregaDias] = NULL
    WHERE [Id] = ''PROV003'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013193459_AddSupplierAndOrderFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013193459_AddSupplierAndOrderFields', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018150000_AddVentasIndicesAndStoredProcedures'
)
BEGIN

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
    END;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018150000_AddVentasIndicesAndStoredProcedures'
)
BEGIN

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
    END;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018150000_AddVentasIndicesAndStoredProcedures'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018150000_AddVentasIndicesAndStoredProcedures', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021163500_UpdateCitasHorarioLaboralValidacion'
)
BEGIN

    CREATE OR ALTER PROCEDURE sp_RegistrarCitaConValidacion
        @UserId      INT,
        @OptometraId INT,
        @Servicio    NVARCHAR(100),
        @FechaHora   DATETIME,
        @Notas       NVARCHAR(500) = NULL,
        @NuevaCitaId INT OUTPUT
    AS
    BEGIN
        SET NOCOUNT ON;
        BEGIN TRANSACTION;

        -- Validar horario laboral: Lunes a Sábado, inicio >= 08:00 y fin (inicio + 60 min) <= 18:00 (último inicio válido: 17:00)
        DECLARE @DiaSemana  INT  = DATEPART(WEEKDAY, @FechaHora);
        DECLARE @HoraInicio TIME = CAST(@FechaHora AS TIME);
        DECLARE @HoraFin    TIME = CAST(DATEADD(MINUTE, 60, @FechaHora) AS TIME);

        IF @DiaSemana = 1 
           OR @HoraInicio < '08:00:00' 
           OR @HoraInicio > '17:00:00'
           OR @HoraFin > '18:00:00'
           OR DATEPART(DAY, DATEADD(MINUTE, 60, @FechaHora)) <> DATEPART(DAY, @FechaHora)
        BEGIN
            ROLLBACK;
            THROW 50001, 'No se puede agendar: las citas solo se permiten de lunes a sabado, entre 8:00 a.m. y 6:00 p.m.', 1;
            RETURN;
        END

        -- Verificar disponibilidad del optometra
        DECLARE @FinNueva DATETIME = DATEADD(MINUTE, 60, @FechaHora);
        IF EXISTS (
            SELECT 1
            FROM   Appointments
            WHERE  Activo = 1
              AND  OptometraId = @OptometraId
              AND  LOWER(Estado) <> 'cancelada'
              AND  FechaHora < @FinNueva
              AND  DATEADD(MINUTE, 60, FechaHora) > @FechaHora
        )
        BEGIN
            ROLLBACK;
            THROW 50002, 'No se puede agendar: el optometra ya tiene una cita en ese horario.', 1;
            RETURN;
        END

        -- Insertar la cita validada
        INSERT INTO Appointments
            (UserId, OptometraId, Servicio, FechaHora, Estado, Notas, FechaCreacion, Activo, VecesReprogramada)
        VALUES
            (@UserId, @OptometraId, @Servicio, @FechaHora, 'pendiente', @Notas, GETUTCDATE(), 1, 0);

        SET @NuevaCitaId = SCOPE_IDENTITY();
        COMMIT;
    END;

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021163500_UpdateCitasHorarioLaboralValidacion'
)
BEGIN

    CREATE OR ALTER TRIGGER trg_Appointment_PreventOverlap
    ON  Appointments
    AFTER INSERT, UPDATE
    AS
    BEGIN
        SET NOCOUNT ON;

        -- Omitir filas inactivas o canceladas sin OptometraId
        IF NOT EXISTS (
            SELECT 1 FROM inserted
            WHERE  Activo = 1 AND LOWER(Estado) <> 'cancelada' AND OptometraId IS NOT NULL
        ) RETURN;

        -- a) Validar horario laboral solo en INSERT o cuando FechaHora cambie
        IF EXISTS (
            SELECT 1 
            FROM   inserted i
            LEFT JOIN deleted d ON d.Id = i.Id
            WHERE  i.Activo = 1
              AND  LOWER(i.Estado) <> 'cancelada'
              AND  (d.Id IS NULL OR d.FechaHora <> i.FechaHora)
              AND  (   DATEPART(WEEKDAY, i.FechaHora) = 1
                   OR  CAST(i.FechaHora AS TIME) < '08:00:00'
                   OR  CAST(i.FechaHora AS TIME) > '17:00:00'
                   OR  CAST(DATEADD(MINUTE, 60, i.FechaHora) AS TIME) > '18:00:00'
                   OR  DATEPART(DAY, DATEADD(MINUTE, 60, i.FechaHora)) <> DATEPART(DAY, i.FechaHora)
                   )
        )
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 50001, 'No se puede agendar: las citas solo se permiten de lunes a sabado, entre 8:00 a.m. y 6:00 p.m.', 1;
        END

        -- b) Validar solapamiento por optometra
        IF EXISTS (
            SELECT 1
            FROM   inserted  i
            JOIN   Appointments a
                   ON  a.OptometraId = i.OptometraId
                   AND a.Activo      = 1
                   AND LOWER(a.Estado) <> 'cancelada'
                   AND a.Id          <> i.Id
            WHERE  i.Activo  = 1
              AND  LOWER(i.Estado) <> 'cancelada'
              AND  i.OptometraId IS NOT NULL
              AND  i.FechaHora < DATEADD(MINUTE, 60, a.FechaHora)
              AND  DATEADD(MINUTE, 60, i.FechaHora) > a.FechaHora
        )
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 50002, 'No se puede agendar: el optometra ya tiene una cita en ese horario.', 1;
        END
    END;

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021163500_UpdateCitasHorarioLaboralValidacion'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261021163500_UpdateCitasHorarioLaboralValidacion', N'9.0.7');
END;

COMMIT;
GO

