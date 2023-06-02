using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Saritasa.Migrations
{
    /// <inheritdoc />
    public partial class UploadFilSecond : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Upload",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Upload");
        }
    }
}
