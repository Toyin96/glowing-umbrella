using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegalSearch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedNewTableToMapSolicitorAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LegalSearchRequests_AspNetUsers_SolicitorId",
                table: "LegalSearchRequests");

            migrationBuilder.RenameColumn(
                name: "SolicitorId",
                table: "LegalSearchRequests",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_LegalSearchRequests_SolicitorId",
                table: "LegalSearchRequests",
                newName: "IX_LegalSearchRequests_UserId");

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedSolicitorId",
                table: "LegalSearchRequests",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NotificationType = table.Column<int>(type: "int", nullable: false),
                    RecipientRole = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecipientUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsBroadcast = table.Column<bool>(type: "bit", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    MetaData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SolicitorAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SolicitorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsAccepted = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitorAssignments", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_LegalSearchRequests_AspNetUsers_UserId",
                table: "LegalSearchRequests",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LegalSearchRequests_AspNetUsers_UserId",
                table: "LegalSearchRequests");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "SolicitorAssignments");

            migrationBuilder.DropColumn(
                name: "AssignedSolicitorId",
                table: "LegalSearchRequests");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "LegalSearchRequests",
                newName: "SolicitorId");

            migrationBuilder.RenameIndex(
                name: "IX_LegalSearchRequests_UserId",
                table: "LegalSearchRequests",
                newName: "IX_LegalSearchRequests_SolicitorId");

            migrationBuilder.AddForeignKey(
                name: "FK_LegalSearchRequests_AspNetUsers_SolicitorId",
                table: "LegalSearchRequests",
                column: "SolicitorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
