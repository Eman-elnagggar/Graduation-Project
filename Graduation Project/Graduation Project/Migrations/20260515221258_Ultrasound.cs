using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graduation_Project.Migrations
{
    /// <inheritdoc />
    public partial class Ultrasound : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ConfidenceScore",
                table: "UltrasoundImages",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPatientUploaded",
                table: "UltrasoundImages",
                type: "bit",
                nullable: false,
                defaultValue: false);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfidenceScore",
                table: "UltrasoundImages");

            migrationBuilder.DropColumn(
                name: "IsPatientUploaded",
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
