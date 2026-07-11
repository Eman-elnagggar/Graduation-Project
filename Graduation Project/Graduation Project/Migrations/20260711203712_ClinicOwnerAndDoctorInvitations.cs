using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graduation_Project.Migrations
{
    /// <inheritdoc />
    public partial class ClinicOwnerAndDoctorInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OwnerDoctorID",
                table: "Clinics",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClinicDoctorInvitations",
                columns: table => new
                {
                    ClinicDoctorInvitationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicID = table.Column<int>(type: "int", nullable: false),
                    InviterDoctorID = table.Column<int>(type: "int", nullable: false),
                    InviteeDoctorID = table.Column<int>(type: "int", nullable: false),
                    InviteeEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicDoctorInvitations", x => x.ClinicDoctorInvitationID);
                    table.ForeignKey(
                        name: "FK_ClinicDoctorInvitations_Clinics_ClinicID",
                        column: x => x.ClinicID,
                        principalTable: "Clinics",
                        principalColumn: "ClinicID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicDoctorInvitations_Doctors_InviteeDoctorID",
                        column: x => x.InviteeDoctorID,
                        principalTable: "Doctors",
                        principalColumn: "DoctorID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicDoctorInvitations_Doctors_InviterDoctorID",
                        column: x => x.InviterDoctorID,
                        principalTable: "Doctors",
                        principalColumn: "DoctorID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clinics_OwnerDoctorID",
                table: "Clinics",
                column: "OwnerDoctorID");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicDoctorInvitations_ClinicID_InviteeDoctorID_Status",
                table: "ClinicDoctorInvitations",
                columns: new[] { "ClinicID", "InviteeDoctorID", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicDoctorInvitations_InviteeDoctorID_Status",
                table: "ClinicDoctorInvitations",
                columns: new[] { "InviteeDoctorID", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicDoctorInvitations_InviterDoctorID",
                table: "ClinicDoctorInvitations",
                column: "InviterDoctorID");

            migrationBuilder.AddForeignKey(
                name: "FK_Clinics_Doctors_OwnerDoctorID",
                table: "Clinics",
                column: "OwnerDoctorID",
                principalTable: "Doctors",
                principalColumn: "DoctorID",
                onDelete: ReferentialAction.Restrict);

            // Clinics created before ownership existed have no owner and would be
            // unmanageable. Hand each one to its first linked doctor.
            migrationBuilder.Sql(@"
                UPDATE c
                SET c.OwnerDoctorID = (
                    SELECT MIN(cd.DoctorID)
                    FROM ClinicDoctors cd
                    WHERE cd.ClinicID = c.ClinicID
                )
                FROM Clinics c
                WHERE c.OwnerDoctorID IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clinics_Doctors_OwnerDoctorID",
                table: "Clinics");

            migrationBuilder.DropTable(
                name: "ClinicDoctorInvitations");

            migrationBuilder.DropIndex(
                name: "IX_Clinics_OwnerDoctorID",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "OwnerDoctorID",
                table: "Clinics");
        }
    }
}
