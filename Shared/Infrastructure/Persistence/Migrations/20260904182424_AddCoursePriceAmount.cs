using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourseCore.Shared.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCoursePriceAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PriceAmount",
                table: "courses",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceAmount",
                table: "courses");
        }
    }
}
