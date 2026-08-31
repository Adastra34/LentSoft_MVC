using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LentSoft.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddHistorialExamenFormulaEntities : Migration
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
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TipoExamen = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OjoDerecho = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OjoIzquierdo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Resultado = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    OptometraId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
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
                        name: "FK_ExamenesVisuales_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FormulasOpticas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EsferaOD = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CilindroOD = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EjeOD = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EsferaOI = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CilindroOI = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EjeOI = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OptometraId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
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
                        name: "FK_FormulasOpticas_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HistorialesClinicos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Diagnostico = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Tratamiento = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OptometraId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
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
                        name: "FK_HistorialesClinicos_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamenesVisuales_OptometraId",
                table: "ExamenesVisuales",
                column: "OptometraId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamenesVisuales_UserId",
                table: "ExamenesVisuales",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulasOpticas_OptometraId",
                table: "FormulasOpticas",
                column: "OptometraId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulasOpticas_UserId",
                table: "FormulasOpticas",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialesClinicos_OptometraId",
                table: "HistorialesClinicos",
                column: "OptometraId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialesClinicos_UserId",
                table: "HistorialesClinicos",
                column: "UserId");
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
