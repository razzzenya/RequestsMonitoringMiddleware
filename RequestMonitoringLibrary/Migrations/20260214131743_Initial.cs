using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RequestMonitoring.Library.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DomainStatusTypes",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomainStatusTypes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "domain",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    host = table.Column<string>(type: "TEXT", nullable: false),
                    status_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_domain", x => x.id);
                    table.ForeignKey(
                        name: "FK_domain_DomainStatusTypes_status_id",
                        column: x => x.status_id,
                        principalTable: "DomainStatusTypes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "DomainStatusTypes",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "Allowed" },
                    { 2, "Greylisted" },
                    { 3, "Unauthorized" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_domain_status_id",
                table: "domain",
                column: "status_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "domain");

            migrationBuilder.DropTable(
                name: "DomainStatusTypes");
        }
    }
}
