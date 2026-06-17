using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graduation_Project.Migrations
{
    /// <inheritdoc />
    public partial class AddPregnancyRecordsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PregnancyRecords",
                columns: table => new
                {
                    PregnancyRecordID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientID = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PregnancyRecords", x => x.PregnancyRecordID);
                    table.ForeignKey(
                        name: "FK_PregnancyRecords_Patients_PatientID",
                        column: x => x.PatientID,
                        principalTable: "Patients",
                        principalColumn: "PatientID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PregnancyRecords_PatientID_StartDate",
                table: "PregnancyRecords",
                columns: new[] { "PatientID", "StartDate" });

            migrationBuilder.Sql(@"
INSERT INTO PregnancyRecords (PatientID, StartDate, EndDate, CreatedAt)
SELECT
    p.PatientID,
    COALESCE(p.DateOfPregnancy, p.LastPregnancyStartedAt) AS StartDate,
    p.PregnancyEndedAt,
    GETDATE()
FROM Patients p
WHERE COALESCE(p.DateOfPregnancy, p.LastPregnancyStartedAt) IS NOT NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PregnancyRecords");
        }
    }
}
