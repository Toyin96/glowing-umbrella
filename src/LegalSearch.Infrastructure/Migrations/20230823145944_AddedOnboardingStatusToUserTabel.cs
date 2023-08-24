using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegalSearch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedOnboardingStatusToUserTabel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OnboardingStatus",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnboardingStatus",
                table: "AspNetUsers");
        }
    }
}
