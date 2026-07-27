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

            migrationBuilder.CreateTable(
                name: "quota",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    domain_id = table.Column<int>(type: "INTEGER", nullable: false),
                    type = table.Column<int>(type: "INTEGER", nullable: false),
                    max_requests = table.Column<int>(type: "INTEGER", nullable: true),
                    period_seconds = table.Column<int>(type: "INTEGER", nullable: true),
                    expires_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    request_count = table.Column<long>(type: "INTEGER", nullable: false),
                    last_reset_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quota", x => x.id);
                    table.ForeignKey(
                        name: "FK_quota_domain_domain_id",
                        column: x => x.domain_id,
                        principalTable: "domain",
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
                    { 3, "Forbidden" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_domain_host",
                table: "domain",
                column: "host",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_domain_status_id",
                table: "domain",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_quota_domain_id",
                table: "quota",
                column: "domain_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quota");

            migrationBuilder.DropTable(
                name: "domain");

            migrationBuilder.DropTable(
                name: "DomainStatusTypes");
        }
    }
}
