using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourseCore.Shared.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTestimonials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "testimonials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Quote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    AvatarUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: true),
                    Published = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_testimonials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_testimonials_courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_testimonials_CourseId",
                table: "testimonials",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_testimonials_Published",
                table: "testimonials",
                column: "Published");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "testimonials");
        }
    }
}
