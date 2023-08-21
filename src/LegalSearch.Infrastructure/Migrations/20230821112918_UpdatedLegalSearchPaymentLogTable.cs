using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegalSearch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedLegalSearchPaymentLogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TranId",
                table: "LegalSearchRequestPaymentLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionStan",
                table: "LegalSearchRequestPaymentLogs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TranId",
                table: "LegalSearchRequestPaymentLogs");

            migrationBuilder.DropColumn(
                name: "TransactionStan",
                table: "LegalSearchRequestPaymentLogs");
        }
    }
}
