using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graduation_Project.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicationReminderSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReminderLeadTimeMinutes",
                table: "Medications",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MedicationReminderSettings",
                columns: table => new
                {
                    MedicationReminderSettingsId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientID = table.Column<int>(type: "int", nullable: false),
                    LeadTimeMinutes = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationReminderSettings", x => x.MedicationReminderSettingsId);
                    table.ForeignKey(
                        name: "FK_MedicationReminderSettings_Patients_PatientID",
                        column: x => x.PatientID,
                        principalTable: "Patients",
                        principalColumn: "PatientID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationReminderSettings_PatientID",
                table: "MedicationReminderSettings",
                column: "PatientID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicationReminderSettings");

            migrationBuilder.DropColumn(
                name: "ReminderLeadTimeMinutes",
                table: "Medications");
        }
    }
}
