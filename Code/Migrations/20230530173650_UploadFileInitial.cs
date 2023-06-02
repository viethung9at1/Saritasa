using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Saritasa.Migrations
{
    /// <inheritdoc />
    public partial class UploadFileInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Upload",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeleteAfterDownload = table.Column<bool>(type: "bit", nullable: false),
                    RegularUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Upload", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Upload_RegularUsers_RegularUserId",
                        column: x => x.RegularUserId,
                        principalTable: "RegularUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Upload_RegularUserId",
                table: "Upload",
                column: "RegularUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Upload");
        }
    }
}
