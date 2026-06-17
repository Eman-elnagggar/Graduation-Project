using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graduation_Project.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicationRefillFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PillsPerDose",
                table: "Medications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalPills",
                table: "Medications",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PillsPerDose",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "TotalPills",
                table: "Medications");
        }
    }
}
