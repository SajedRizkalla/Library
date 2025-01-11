using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyLibrary.Data.Migrations
{
    /// <inheritdoc />
    public partial class bookRentQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "quantity",
                table: "Books",
                newName: "SellQuantity");

            migrationBuilder.RenameColumn(
                name: "Sale",
                table: "Books",
                newName: "SalePercentage");

            migrationBuilder.AddColumn<string>(
                name: "DownloadFormats",
                table: "Books",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "DownloadUrl",
                table: "Books",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RentQuantity",
                table: "Books",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SaleEndDate",
                table: "Books",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DownloadFormats",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "DownloadUrl",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "RentQuantity",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "SaleEndDate",
                table: "Books");

            migrationBuilder.RenameColumn(
                name: "SellQuantity",
                table: "Books",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "SalePercentage",
                table: "Books",
                newName: "Sale");
        }
    }
}
