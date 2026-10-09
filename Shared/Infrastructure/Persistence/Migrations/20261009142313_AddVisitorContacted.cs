using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourseCore.Shared.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitorContacted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ContactedAt",
                table: "visitors",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ContactedByUserId",
                table: "visitors",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_visitors_ContactedAt",
                table: "visitors",
                column: "ContactedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_visitors_ContactedAt",
                table: "visitors");

            migrationBuilder.DropColumn(
                name: "ContactedAt",
                table: "visitors");

            migrationBuilder.DropColumn(
                name: "ContactedByUserId",
                table: "visitors");
        }
    }
}
