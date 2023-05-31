using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Saritasa.Migrations
{
    /// <inheritdoc />
    public partial class UploadsToDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Upload_RegularUsers_RegularUserId",
                table: "Upload");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Upload",
                table: "Upload");

            migrationBuilder.RenameTable(
                name: "Upload",
                newName: "Uploads");

            migrationBuilder.RenameIndex(
                name: "IX_Upload_RegularUserId",
                table: "Uploads",
                newName: "IX_Uploads_RegularUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Uploads",
                table: "Uploads",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Uploads_RegularUsers_RegularUserId",
                table: "Uploads",
                column: "RegularUserId",
                principalTable: "RegularUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Uploads_RegularUsers_RegularUserId",
                table: "Uploads");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Uploads",
                table: "Uploads");

            migrationBuilder.RenameTable(
                name: "Uploads",
                newName: "Upload");

            migrationBuilder.RenameIndex(
                name: "IX_Uploads_RegularUserId",
                table: "Upload",
                newName: "IX_Upload_RegularUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Upload",
                table: "Upload",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Upload_RegularUsers_RegularUserId",
                table: "Upload",
                column: "RegularUserId",
                principalTable: "RegularUsers",
                principalColumn: "Id");
        }
    }
}
