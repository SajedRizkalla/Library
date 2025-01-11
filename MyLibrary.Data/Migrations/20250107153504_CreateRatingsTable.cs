using Microsoft.EntityFrameworkCore.Migrations;

namespace MyLibrary.Data.Migrations
{
    public partial class CreateRatingsTable : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            
            migrationBuilder.CreateTable(
                name: "Ratings",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"), // Primary Key with auto-increment
                    RatingValue = table.Column<int>(nullable: false), // Star rating (1-5)
                    Feedback = table.Column<string>(nullable: true), // Optional user feedback
                    Timestamp = table.Column<DateTime>(nullable: false, defaultValueSql: "GETDATE()"), // Default timestamp
                    IsWebsiteRating = table.Column<bool>(nullable: false), // Discriminator column (true for website ratings)
                    BookId = table.Column<string>(type: "nvarchar(450)", nullable: true) // Foreign Key to Books table
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ratings", x => x.Id); // Define the primary key
                    table.ForeignKey(
                        name: "FK_Ratings_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade); // Cascade delete book ratings when the book is deleted
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_BookId",
                table: "Ratings",
                column: "BookId"); // Index for faster lookups on BookId
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Ratings");
        }
    }
}