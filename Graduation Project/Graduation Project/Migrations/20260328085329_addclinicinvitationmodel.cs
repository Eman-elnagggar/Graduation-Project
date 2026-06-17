using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graduation_Project.Migrations
{
    /// <inheritdoc />
    public partial class addclinicinvitationmodel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClinicInvitations",
                columns: table => new
                {
                    ClinicInvitationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorID = table.Column<int>(type: "int", nullable: false),
                    ClinicID = table.Column<int>(type: "int", nullable: false),
                    AssistantID = table.Column<int>(type: "int", nullable: false),
                    AssistantEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResponseMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicInvitations", x => x.ClinicInvitationID);
                    table.ForeignKey(
                        name: "FK_ClinicInvitations_Assistants_AssistantID",
                        column: x => x.AssistantID,
                        principalTable: "Assistants",
                        principalColumn: "AssistantID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicInvitations_Clinics_ClinicID",
                        column: x => x.ClinicID,
                        principalTable: "Clinics",
                        principalColumn: "ClinicID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicInvitations_Doctors_DoctorID",
                        column: x => x.DoctorID,
                        principalTable: "Doctors",
                        principalColumn: "DoctorID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicInvitations_AssistantID",
                table: "ClinicInvitations",
                column: "AssistantID");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicInvitations_ClinicID",
                table: "ClinicInvitations",
                column: "ClinicID");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicInvitations_DoctorID_ClinicID_AssistantID_Status",
                table: "ClinicInvitations",
                columns: new[] { "DoctorID", "ClinicID", "AssistantID", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicInvitations");
        }
    }
}
