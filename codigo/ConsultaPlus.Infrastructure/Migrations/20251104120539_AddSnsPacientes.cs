using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsultaPlus.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSnsPacientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SnsPacientes",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NUtente = table.Column<string>(maxLength: 50, nullable: false),
                    NomeCompleto = table.Column<string>(maxLength: 200, nullable: false),
                    Nif = table.Column<string>(maxLength: 50, nullable: false),
                    Telemovel = table.Column<string>(maxLength: 50, nullable: false),
                    Morada = table.Column<string>(maxLength: 400, nullable: false),
                    Email = table.Column<string>(maxLength: 256, nullable: false),
                    DataNascimento = table.Column<DateTime>(nullable: false),
                    DataCriacao = table.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnsPacientes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_SnsPacientes_NUtente",
                table: "SnsPacientes",
                column: "NUtente",
                unique: true);
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SnsPacientes");
        }

    }
}
