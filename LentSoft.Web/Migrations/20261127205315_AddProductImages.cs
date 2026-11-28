using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LentSoft.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddProductImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                // ── SQL Server: T-SQL idempotente ──
                migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[ProductImages]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProductImages] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [ProductId]     INT NOT NULL,
        [ImagenUrl]     NVARCHAR(500) NOT NULL,
        [Orden]         INT NOT NULL DEFAULT 0,
        [FechaCreacion] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_ProductImages] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_ProductImages_Products_ProductId]
            FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END;");

                migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_ProductImages_ProductId'
               AND object_id = OBJECT_ID('ProductImages'))
BEGIN
    CREATE INDEX [IX_ProductImages_ProductId] ON [ProductImages] ([ProductId]);
END;");
            }
            else
            {
                // ── SQLite: sin cambios (original) ──
                migrationBuilder.CreateTable(
                    name: "ProductImages",
                    columns: table => new
                    {
                        Id = table.Column<int>(type: "INTEGER", nullable: false)
                            .Annotation("Sqlite:Autoincrement", true),
                        ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                        ImagenUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                        Orden = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                        FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_ProductImages", x => x.Id);
                        table.ForeignKey(
                            name: "FK_ProductImages_Products_ProductId",
                            column: x => x.ProductId,
                            principalTable: "Products",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade);
                    });

                migrationBuilder.CreateIndex(
                    name: "IX_ProductImages_ProductId",
                    table: "ProductImages",
                    column: "ProductId");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE name = 'IX_ProductImages_ProductId'
           AND object_id = OBJECT_ID('ProductImages'))
    DROP INDEX [IX_ProductImages_ProductId] ON [ProductImages];
IF OBJECT_ID(N'[dbo].[ProductImages]', N'U') IS NOT NULL
    DROP TABLE [dbo].[ProductImages];");
            }
            else
            {
                migrationBuilder.DropTable(name: "ProductImages");
            }
        }
    }
}
