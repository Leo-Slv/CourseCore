using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourseCore.Shared.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTestimonialSubmittedByUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedByUserId",
                table: "testimonials",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_testimonials_SubmittedByUserId",
                table: "testimonials",
                column: "SubmittedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_testimonials_users_SubmittedByUserId",
                table: "testimonials",
                column: "SubmittedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_testimonials_users_SubmittedByUserId",
                table: "testimonials");

            migrationBuilder.DropIndex(
                name: "IX_testimonials_SubmittedByUserId",
                table: "testimonials");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                table: "testimonials");
        }
    }
}
