using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourseCore.Shared.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAreaAccentColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccentColor",
                table: "areas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Blue");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccentColor",
                table: "areas");
        }
    }
}
