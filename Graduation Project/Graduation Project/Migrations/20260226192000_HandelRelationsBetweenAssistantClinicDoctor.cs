using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graduation_Project.Migrations
{
    /// <inheritdoc />
    public partial class HandelRelationsBetweenAssistantClinicDoctor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clinics_Doctors_DoctorID",
                table: "Clinics");

            migrationBuilder.DropIndex(
                name: "IX_Clinics_DoctorID",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "DoctorID",
                table: "Clinics");

            migrationBuilder.CreateTable(
                name: "AssistantDoctors",
                columns: table => new
                {
                    AssistantID = table.Column<int>(type: "int", nullable: false),
                    DoctorID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssistantDoctors", x => new { x.AssistantID, x.DoctorID });
                    table.ForeignKey(
                        name: "FK_AssistantDoctors_Assistants_AssistantID",
                        column: x => x.AssistantID,
                        principalTable: "Assistants",
                        principalColumn: "AssistantID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssistantDoctors_Doctors_DoctorID",
                        column: x => x.DoctorID,
                        principalTable: "Doctors",
                        principalColumn: "DoctorID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClinicDoctors",
                columns: table => new
                {
                    ClinicID = table.Column<int>(type: "int", nullable: false),
                    DoctorID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicDoctors", x => new { x.ClinicID, x.DoctorID });
                    table.ForeignKey(
                        name: "FK_ClinicDoctors_Clinics_ClinicID",
                        column: x => x.ClinicID,
                        principalTable: "Clinics",
                        principalColumn: "ClinicID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicDoctors_Doctors_DoctorID",
                        column: x => x.DoctorID,
                        principalTable: "Doctors",
                        principalColumn: "DoctorID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssistantDoctors_DoctorID",
                table: "AssistantDoctors",
                column: "DoctorID");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicDoctors_DoctorID",
                table: "ClinicDoctors",
                column: "DoctorID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssistantDoctors");

            migrationBuilder.DropTable(
                name: "ClinicDoctors");

            migrationBuilder.AddColumn<int>(
                name: "DoctorID",
                table: "Clinics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Clinics_DoctorID",
                table: "Clinics",
                column: "DoctorID");

            migrationBuilder.AddForeignKey(
                name: "FK_Clinics_Doctors_DoctorID",
                table: "Clinics",
                column: "DoctorID",
                principalTable: "Doctors",
                principalColumn: "DoctorID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
