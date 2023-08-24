using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegalSearch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SynchronizedDatabaseTablesToCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_AspNetUsers_Firms_FirmId1",
            //    table: "AspNetUsers");

            //migrationBuilder.DropIndex(
            //    name: "IX_AspNetUsers_FirmId1",
            //    table: "AspNetUsers");

            //migrationBuilder.DropColumn(
            //    name: "FirmId1",
            //    table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FirmId1",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_FirmId1",
                table: "AspNetUsers",
                column: "FirmId1");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Firms_FirmId1",
                table: "AspNetUsers",
                column: "FirmId1",
                principalTable: "Firms",
                principalColumn: "Id");
        }
    }
}
