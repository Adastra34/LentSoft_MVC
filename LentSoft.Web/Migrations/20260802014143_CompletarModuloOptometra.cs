using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LentSoft.Web.Migrations
{
    /// <inheritdoc />
    public partial class CompletarModuloOptometra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AniosExperiencia",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Direccion",
                table: "Users",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EPS",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EspecialidadDetalle",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstadoPaciente",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaNacimiento",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FotoUrl",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Genero",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObservacionesPaciente",
                table: "Users",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistroMedico",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Universidad",
                table: "Users",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Antecedentes",
                table: "HistorialesClinicos",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "HistorialesClinicos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExamenesRealizados",
                table: "HistorialesClinicos",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Observaciones",
                table: "HistorialesClinicos",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DistanciaPupilar",
                table: "FormulasOpticas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoLente",
                table: "FormulasOpticas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AdicionOD",
                table: "ExamenesVisuales",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdicionOI",
                table: "ExamenesVisuales",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CilindroOD",
                table: "ExamenesVisuales",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CilindroOI",
                table: "ExamenesVisuales",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Diagnostico",
                table: "ExamenesVisuales",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EjeOD",
                table: "ExamenesVisuales",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EjeOI",
                table: "ExamenesVisuales",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EsferaOD",
                table: "ExamenesVisuales",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EsferaOI",
                table: "ExamenesVisuales",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Observaciones",
                table: "ExamenesVisuales",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SegmentoAnterior",
                table: "ExamenesVisuales",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SegmentoPosterior",
                table: "ExamenesVisuales",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TonometriaOD",
                table: "ExamenesVisuales",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TonometriaOI",
                table: "ExamenesVisuales",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tratamiento",
                table: "ExamenesVisuales",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AniosExperiencia", "Direccion", "EPS", "EspecialidadDetalle", "EstadoPaciente", "FechaNacimiento", "FotoUrl", "Genero", "ObservacionesPaciente", "RegistroMedico", "Universidad" },
                values: new object[] { null, null, null, null, "Activo", null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "AniosExperiencia", "Direccion", "EPS", "EspecialidadDetalle", "EstadoPaciente", "FechaNacimiento", "FotoUrl", "Genero", "ObservacionesPaciente", "RegistroMedico", "Universidad" },
                values: new object[] { null, null, null, null, "Activo", null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "AniosExperiencia", "Direccion", "EPS", "EspecialidadDetalle", "EstadoPaciente", "FechaNacimiento", "FotoUrl", "Genero", "ObservacionesPaciente", "RegistroMedico", "Universidad" },
                values: new object[] { null, null, null, null, "Activo", null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "AniosExperiencia", "Direccion", "EPS", "EspecialidadDetalle", "EstadoPaciente", "FechaNacimiento", "FotoUrl", "Genero", "ObservacionesPaciente", "RegistroMedico", "Universidad" },
                values: new object[] { null, null, null, null, "Activo", null, null, null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AniosExperiencia",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Direccion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EPS",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EspecialidadDetalle",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EstadoPaciente",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FechaNacimiento",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FotoUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Genero",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ObservacionesPaciente",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RegistroMedico",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Universidad",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Antecedentes",
                table: "HistorialesClinicos");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "HistorialesClinicos");

            migrationBuilder.DropColumn(
                name: "ExamenesRealizados",
                table: "HistorialesClinicos");

            migrationBuilder.DropColumn(
                name: "Observaciones",
                table: "HistorialesClinicos");

            migrationBuilder.DropColumn(
                name: "DistanciaPupilar",
                table: "FormulasOpticas");

            migrationBuilder.DropColumn(
                name: "TipoLente",
                table: "FormulasOpticas");

            migrationBuilder.DropColumn(
                name: "AdicionOD",
                table: "ExamenesVisuales");

            migrationBuilder.DropColumn(
                name: "AdicionOI",
                table: "ExamenesVisuales");

            migrationBuilder.DropColumn(
                name: "CilindroOD",
                table: "ExamenesVisuales");

            migrationBuilder.DropColumn(
                name: "CilindroOI",
                table: "ExamenesVisuales");

            migrationBuilder.DropColumn(
                name: "Diagnostico",
                table: "ExamenesVisuales");

            migrationBuilder.DropColumn(
                name: "EjeOD",
                table: "ExamenesVisuales");

            migrationBuilder.DropColumn(
                name: "EjeOI",
                table: "ExamenesVisuales");

            migrationBuilder.DropColumn(
                name: "EsferaOD",
                table: "ExamenesVisuales");

            migrationBuilder.DropColumn(
                name: "EsferaOI",
                table: "ExamenesVisuales");

            migrationBuilder.DropColumn(
                name: "Observaciones",
                table: "ExamenesVisuales");

            migrationBuilder.DropColumn(
                name: "SegmentoAnterior",
                table: "ExamenesVisuales");

            migrationBuilder.DropColumn(
                name: "SegmentoPosterior",
                table: "ExamenesVisuales");

            migrationBuilder.DropColumn(
                name: "TonometriaOD",
                table: "ExamenesVisuales");

            migrationBuilder.DropColumn(
                name: "TonometriaOI",
                table: "ExamenesVisuales");

            migrationBuilder.DropColumn(
                name: "Tratamiento",
                table: "ExamenesVisuales");
        }
    }
}
