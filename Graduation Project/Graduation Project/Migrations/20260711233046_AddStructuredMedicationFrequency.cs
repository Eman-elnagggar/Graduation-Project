using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graduation_Project.Migrations
{
    /// <inheritdoc />
    public partial class AddStructuredMedicationFrequency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Form",
                table: "Medications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FrequencyCode",
                table: "Medications",
                type: "nvarchar(max)",
                nullable: true);

            // Existing medications predate the structured schedule. They were all
            // implicitly daily, so default to 1 — a 0 here would be read as
            // "taken only as needed" and would silently stop their reminders.
            migrationBuilder.AddColumn<int>(
                name: "IntervalDays",
                table: "Medications",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TimesPerDay",
                table: "Medications",
                type: "int",
                nullable: false,
                defaultValue: 1);

            // Backfill the real dose count from each medication's saved schedule rows.
            migrationBuilder.Sql(@"
                UPDATE m
                SET m.TimesPerDay = s.DoseCount
                FROM Medications m
                INNER JOIN (
                    SELECT MedicationId, COUNT(*) AS DoseCount
                    FROM MedicationSchedules
                    GROUP BY MedicationId
                ) s ON s.MedicationId = m.MedicationId;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Form",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "FrequencyCode",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "IntervalDays",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "TimesPerDay",
                table: "Medications");
        }
    }
}
