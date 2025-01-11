using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyLibrary.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    username = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    password = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    gender = table.Column<string>(type: "nvarchar(max)", defaultValue: "Not Specified", nullable: false)

                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });
            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    Id = table.Column<string>(maxLength: 450, nullable: false),
                    Cover = table.Column<string>(maxLength: 4000, nullable: false),
                    Author = table.Column<string>(maxLength: 4000, nullable: false),
                    Title = table.Column<string>(maxLength: 4000, nullable: false),
                    Publisher = table.Column<string>(maxLength: 4000, nullable: false),
                    Borrowprice = table.Column<string>(maxLength: 4000, nullable: false),
                    Buyprice = table.Column<string>(maxLength: 4000, nullable: false),
                    Year = table.Column<string>(maxLength: 4, nullable: false),
                    Sale = table.Column<string>(maxLength: 100, nullable: true), // Nullable as per class
                    quantity= table.Column<string>(type: "int", defaultValue: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                });


        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");
            migrationBuilder.DropTable(
                name: "Books");
        }
    }
}
