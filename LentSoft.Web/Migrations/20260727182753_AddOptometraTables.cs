using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LentSoft.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddOptometraTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExamenesVisuales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PacienteId = table.Column<int>(type: "int", nullable: false),
                    OptometraId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    TipoExamen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OjoDerecho = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OjoIzquierdo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Resultado = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamenesVisuales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamenesVisuales_Users_OptometraId",
                        column: x => x.OptometraId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExamenesVisuales_Users_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FormulasOpticas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PacienteId = table.Column<int>(type: "int", nullable: false),
                    OptometraId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    EsferaOD = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CilindroOD = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EjeOD = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EsferaOI = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CilindroOI = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EjeOI = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Adicion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DistanciaPupilar = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormulasOpticas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormulasOpticas_Users_OptometraId",
                        column: x => x.OptometraId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormulasOpticas_Users_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HistorialesClinicos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PacienteId = table.Column<int>(type: "int", nullable: false),
                    OptometraId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Diagnostico = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Tratamiento = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialesClinicos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialesClinicos_Users_OptometraId",
                        column: x => x.OptometraId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HistorialesClinicos_Users_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "ExamenesVisuales",
                columns: new[] { "Id", "Fecha", "OjoDerecho", "OjoIzquierdo", "OptometraId", "PacienteId", "Resultado", "TipoExamen" },
                values: new object[] { 1, new DateTime(2026, 6, 1, 10, 0, 0, 0, DateTimeKind.Utc), "20/25", "20/30", 3, 2, "Requiere corrección óptica", "Agudeza Visual" });

            migrationBuilder.InsertData(
                table: "FormulasOpticas",
                columns: new[] { "Id", "Adicion", "CilindroOD", "CilindroOI", "DistanciaPupilar", "EjeOD", "EjeOI", "EsferaOD", "EsferaOI", "Fecha", "Observaciones", "OptometraId", "PacienteId" },
                values: new object[] { 1, null, "-0.50", "-0.75", null, "180°", "175°", "-1.25", "-1.50", new DateTime(2026, 6, 1, 10, 0, 0, 0, DateTimeKind.Utc), "Uso permanente. Control en 6 meses.", 3, 2 });

            migrationBuilder.InsertData(
                table: "HistorialesClinicos",
                columns: new[] { "Id", "Diagnostico", "Fecha", "Observaciones", "OptometraId", "PacienteId", "Tratamiento" },
                values: new object[] { 1, "Miopía leve OD -1.25 OI -1.50", new DateTime(2026, 6, 1, 10, 0, 0, 0, DateTimeKind.Utc), "Paciente refiere fatiga visual al trabajar frente al computador.", 3, 2, "Lentes correctivos" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamenesVisuales_OptometraId",
                table: "ExamenesVisuales",
                column: "OptometraId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamenesVisuales_PacienteId",
                table: "ExamenesVisuales",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulasOpticas_OptometraId",
                table: "FormulasOpticas",
                column: "OptometraId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulasOpticas_PacienteId",
                table: "FormulasOpticas",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialesClinicos_OptometraId",
                table: "HistorialesClinicos",
                column: "OptometraId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialesClinicos_PacienteId",
                table: "HistorialesClinicos",
                column: "PacienteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamenesVisuales");

            migrationBuilder.DropTable(
                name: "FormulasOpticas");

            migrationBuilder.DropTable(
                name: "HistorialesClinicos");
        }
    }
}
