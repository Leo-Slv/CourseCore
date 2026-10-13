using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourseCore.Shared.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonProgressLastPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastPositionSeconds",
                table: "user_lesson_progress",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastPositionSeconds",
                table: "user_lesson_progress");
        }
    }
}
