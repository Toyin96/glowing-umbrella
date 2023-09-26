using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegalSearch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fixedDataIntegrityConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ZonalServiceManagerId",
                table: "Branches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ZonalServiceManagers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmailAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZonalServiceManagers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Branches_ZonalServiceManagerId",
                table: "Branches",
                column: "ZonalServiceManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Branches_ZonalServiceManagers_ZonalServiceManagerId",
                table: "Branches",
                column: "ZonalServiceManagerId",
                principalTable: "ZonalServiceManagers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Branches_ZonalServiceManagers_ZonalServiceManagerId",
                table: "Branches");

            migrationBuilder.DropTable(
                name: "ZonalServiceManagers");

            migrationBuilder.DropIndex(
                name: "IX_Branches_ZonalServiceManagerId",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "ZonalServiceManagerId",
                table: "Branches");
        }
    }
}
