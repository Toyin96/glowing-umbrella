using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegalSearch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedFirmNameUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Firms_Name",
                table: "Firms");

            migrationBuilder.CreateIndex(
                name: "IX_Firms_Name",
                table: "Firms",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Firms_Name",
                table: "Firms");

            migrationBuilder.CreateIndex(
                name: "IX_Firms_Name",
                table: "Firms",
                column: "Name",
                unique: true);
        }
    }
}
