using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegalSearch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addedAlternateEmailAddressToZsm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlternateEmailAddress",
                table: "ZonalServiceManagers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlternateEmailAddress",
                table: "ZonalServiceManagers");
        }
    }
}
