using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegalSearch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedRequestTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupportingDocuments_LegalSearchRequests_LegalRequestId",
                table: "SupportingDocuments");

            migrationBuilder.AddForeignKey(
                name: "FK_SupportingDocuments_LegalSearchRequests_LegalRequestId",
                table: "SupportingDocuments",
                column: "LegalRequestId",
                principalTable: "LegalSearchRequests",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupportingDocuments_LegalSearchRequests_LegalRequestId",
                table: "SupportingDocuments");

            migrationBuilder.AddForeignKey(
                name: "FK_SupportingDocuments_LegalSearchRequests_LegalRequestId",
                table: "SupportingDocuments",
                column: "LegalRequestId",
                principalTable: "LegalSearchRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
