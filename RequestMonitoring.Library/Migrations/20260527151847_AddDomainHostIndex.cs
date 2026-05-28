using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequestMonitoring.Library.Migrations
{
    /// <inheritdoc />
    public partial class AddDomainHostIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_domain_host",
                table: "domain",
                column: "host",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_domain_host",
                table: "domain");
        }
    }
}
