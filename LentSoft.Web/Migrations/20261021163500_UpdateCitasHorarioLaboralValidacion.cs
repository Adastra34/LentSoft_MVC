using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LentSoft.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCitasHorarioLaboralValidacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. sp_RegistrarCitaConValidacion (SQL Server) ──
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
");

            // ── 2. trg_Appointment_PreventOverlap (SQL Server) ──
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
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ── 1. Restaurar sp_RegistrarCitaConValidacion anterior (20260826045418) ──
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

            // ── 2. Restaurar trg_Appointment_PreventOverlap anterior (20260826045418) ──
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
        }
    }
}
