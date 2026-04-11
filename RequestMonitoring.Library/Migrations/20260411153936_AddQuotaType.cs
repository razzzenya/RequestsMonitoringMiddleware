using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequestMonitoring.Library.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotaType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "quota",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "type",
                table: "quota");
        }
    }
}
