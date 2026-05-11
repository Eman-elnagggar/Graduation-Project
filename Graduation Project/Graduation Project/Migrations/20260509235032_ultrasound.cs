using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graduation_Project.Migrations
{
    /// <inheritdoc />
    public partial class ultrasound : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ConfidenceScore",
                table: "UltrasoundImages",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalImagePath",
                table: "UltrasoundImages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Prediction",
                table: "UltrasoundImages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResultImagePath",
                table: "UltrasoundImages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "UltrasoundImages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_UltrasoundImages_Status",
                table: "UltrasoundImages",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UltrasoundImages_UploadDate",
                table: "UltrasoundImages",
                column: "UploadDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UltrasoundImages_Status",
                table: "UltrasoundImages");

            migrationBuilder.DropIndex(
                name: "IX_UltrasoundImages_UploadDate",
                table: "UltrasoundImages");

            migrationBuilder.DropColumn(
                name: "ConfidenceScore",
                table: "UltrasoundImages");

            migrationBuilder.DropColumn(
                name: "OriginalImagePath",
                table: "UltrasoundImages");

            migrationBuilder.DropColumn(
                name: "Prediction",
                table: "UltrasoundImages");

            migrationBuilder.DropColumn(
                name: "ResultImagePath",
                table: "UltrasoundImages");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "UltrasoundImages");
        }
    }
}
