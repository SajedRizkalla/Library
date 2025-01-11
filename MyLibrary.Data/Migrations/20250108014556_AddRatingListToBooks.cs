using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyLibrary.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRatingListToBooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure the column is created before altering it
            migrationBuilder.AddColumn<string>(
                name: "RatingListJson",
                table: "Books",
                type: "nvarchar(max)",
                nullable: true); // Set nullable to true if it's optional
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the column if rolling back
            migrationBuilder.DropColumn(
                name: "RatingListJson",
                table: "Books");
        }

    }
}
