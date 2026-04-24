using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequestMonitoring.Library.Migrations
{
    /// <inheritdoc />
    public partial class AddQuota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quota",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    domain_id = table.Column<int>(type: "INTEGER", nullable: false),
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
        }
    }
}
