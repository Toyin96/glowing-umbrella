using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegalSearch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedSolIdToNotificationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SolId",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SolId",
                table: "Notifications");
        }
    }
}
