using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourseCore.Shared.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseIsFeatured : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "courses",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "courses");
        }
    }
}
