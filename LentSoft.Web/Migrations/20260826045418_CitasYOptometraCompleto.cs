using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LentSoft.Web.Migrations
{
    /// <inheritdoc />
    public partial class CitasYOptometraCompleto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OptometraId",
                table: "Appointments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VecesReprogramada",
                table: "Appointments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AuditoriaCitas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppointmentId = table.Column<int>(type: "int", nullable: false),
                    EstadoAnterior = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EstadoNuevo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaCambio = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditoriaCitas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditoriaCitas_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumeroPedido = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClienteNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ProductoNombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Notas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumeroPedido = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SupplierId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Notas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierOrders_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Appointments",
                keyColumn: "Id",
                keyValue: 1,
                column: "OptometraId",
                value: 3);

            migrationBuilder.UpdateData(
                table: "Appointments",
                keyColumn: "Id",
                keyValue: 2,
                column: "OptometraId",
                value: 3);

            migrationBuilder.InsertData(
                table: "SalesOrders",
                columns: new[] { "Id", "Activo", "Cantidad", "ClienteNombre", "Estado", "Fecha", "Notas", "NumeroPedido", "PrecioUnitario", "ProductoNombre", "Total" },
                values: new object[,]
                {
                    { 1, true, 2, "Carlos Ram�rez", "entregado", new DateTime(2026, 5, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, "PED-VENTA-001", 350000.00m, "Gafas de Sol Polarizadas Especiales", 700000.00m },
                    { 2, true, 1, "Ana Mar�a Torres", "enviado", new DateTime(2026, 5, 22, 0, 0, 0, 0, DateTimeKind.Utc), null, "PED-VENTA-002", 480000.00m, "Lentes de Contacto Toricos Custom", 480000.00m }
                });

            migrationBuilder.InsertData(
                table: "SupplierOrders",
                columns: new[] { "Id", "Activo", "Cantidad", "Estado", "Fecha", "Notas", "NumeroPedido", "PrecioUnitario", "ProductId", "SupplierId", "Total" },
                values: new object[,]
                {
                    { 1, true, 15, "recibido", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, "PED-PROV-001", 1200000.00m, 1, "PROV001", 18000000.00m },
                    { 2, true, 30, "pendiente", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), null, "PED-PROV-002", 200000.00m, 2, "PROV002", 6000000.00m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_OptometraId",
                table: "Appointments",
                column: "OptometraId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaCitas_AppointmentId",
                table: "AuditoriaCitas",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaCitas_FechaCambio",
                table: "AuditoriaCitas",
                column: "FechaCambio",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_NumeroPedido",
                table: "SalesOrders",
                column: "NumeroPedido");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierOrders_NumeroPedido",
                table: "SupplierOrders",
                column: "NumeroPedido");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierOrders_ProductId",
                table: "SupplierOrders",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierOrders_SupplierId",
                table: "SupplierOrders",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Users_OptometraId",
                table: "Appointments",
                column: "OptometraId",
                principalTable: "Users",
                principalColumn: "Id");
            // ──────────────────────────────────────────────────────────
            // STORED PROCEDURES
            // ──────────────────────────────────────────────────────────

            // 1. sp_VerificarDisponibilidadCita
            migrationBuilder.Sql(@"
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
");

            // 2. sp_HistorialCompletoPaciente
            migrationBuilder.Sql(@"
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
");

            // 3. sp_ReporteCitasPorEstado
            migrationBuilder.Sql(@"
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
");

            // 4. sp_RegistrarCitaConValidacion
            migrationBuilder.Sql(@"
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
");

            // ──────────────────────────────────────────────────────────
            // TRIGGERS
            // ──────────────────────────────────────────────────────────

            // 1. trg_Appointment_PreventOverlap
            migrationBuilder.Sql(@"
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

    -- b) Validar solapamiento por optometra (solo si pasó la validación de horario)
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
");

            // 2. trg_Appointment_Auditoria
            migrationBuilder.Sql(@"
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
");

            // 3. trg_HistorialClinico_PrevenirEliminacionConFormula
            migrationBuilder.Sql(@"
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
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Eliminar triggers (orden inverso)
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_HistorialClinico_PrevenirEliminacionConFormula;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_Appointment_Auditoria;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_Appointment_PreventOverlap;");

            // Eliminar stored procedures
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_RegistrarCitaConValidacion;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_ReporteCitasPorEstado;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_HistorialCompletoPaciente;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_VerificarDisponibilidadCita;");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Users_OptometraId",
                table: "Appointments");

            migrationBuilder.DropTable(
                name: "AuditoriaCitas");

            migrationBuilder.DropTable(
                name: "SalesOrders");

            migrationBuilder.DropTable(
                name: "SupplierOrders");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_OptometraId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "OptometraId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "VecesReprogramada",
                table: "Appointments");
        }
    }
}

