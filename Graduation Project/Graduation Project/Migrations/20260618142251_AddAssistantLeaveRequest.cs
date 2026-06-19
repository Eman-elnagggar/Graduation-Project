using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graduation_Project.Migrations
{
    /// <inheritdoc />
    public partial class AddAssistantLeaveRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssistantLeaveRequests",
                columns: table => new
                {
                    AssistantLeaveRequestID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssistantID = table.Column<int>(type: "int", nullable: false),
                    OldClinicID = table.Column<int>(type: "int", nullable: false),
                    NewClinicID = table.Column<int>(type: "int", nullable: false),
                    NewDoctorID = table.Column<int>(type: "int", nullable: false),
                    ClinicInvitationID = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolutionMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssistantLeaveRequests", x => x.AssistantLeaveRequestID);
                    table.ForeignKey(
                        name: "FK_AssistantLeaveRequests_Assistants_AssistantID",
                        column: x => x.AssistantID,
                        principalTable: "Assistants",
                        principalColumn: "AssistantID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssistantLeaveRequests_ClinicInvitations_ClinicInvitationID",
                        column: x => x.ClinicInvitationID,
                        principalTable: "ClinicInvitations",
                        principalColumn: "ClinicInvitationID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssistantLeaveRequests_Clinics_NewClinicID",
                        column: x => x.NewClinicID,
                        principalTable: "Clinics",
                        principalColumn: "ClinicID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssistantLeaveRequests_Clinics_OldClinicID",
                        column: x => x.OldClinicID,
                        principalTable: "Clinics",
                        principalColumn: "ClinicID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssistantLeaveRequests_Doctors_NewDoctorID",
                        column: x => x.NewDoctorID,
                        principalTable: "Doctors",
                        principalColumn: "DoctorID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssistantLeaveApprovals",
                columns: table => new
                {
                    AssistantLeaveApprovalID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssistantLeaveRequestID = table.Column<int>(type: "int", nullable: false),
                    DoctorID = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RespondedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssistantLeaveApprovals", x => x.AssistantLeaveApprovalID);
                    table.ForeignKey(
                        name: "FK_AssistantLeaveApprovals_AssistantLeaveRequests_AssistantLeaveRequestID",
                        column: x => x.AssistantLeaveRequestID,
                        principalTable: "AssistantLeaveRequests",
                        principalColumn: "AssistantLeaveRequestID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssistantLeaveApprovals_Doctors_DoctorID",
                        column: x => x.DoctorID,
                        principalTable: "Doctors",
                        principalColumn: "DoctorID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssistantLeaveApprovals_AssistantLeaveRequestID",
                table: "AssistantLeaveApprovals",
                column: "AssistantLeaveRequestID");

            migrationBuilder.CreateIndex(
                name: "IX_AssistantLeaveApprovals_DoctorID_Status",
                table: "AssistantLeaveApprovals",
                columns: new[] { "DoctorID", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AssistantLeaveRequests_AssistantID_Status",
                table: "AssistantLeaveRequests",
                columns: new[] { "AssistantID", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AssistantLeaveRequests_ClinicInvitationID",
                table: "AssistantLeaveRequests",
                column: "ClinicInvitationID");

            migrationBuilder.CreateIndex(
                name: "IX_AssistantLeaveRequests_NewClinicID",
                table: "AssistantLeaveRequests",
                column: "NewClinicID");

            migrationBuilder.CreateIndex(
                name: "IX_AssistantLeaveRequests_NewDoctorID",
                table: "AssistantLeaveRequests",
                column: "NewDoctorID");

            migrationBuilder.CreateIndex(
                name: "IX_AssistantLeaveRequests_OldClinicID",
                table: "AssistantLeaveRequests",
                column: "OldClinicID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssistantLeaveApprovals");

            migrationBuilder.DropTable(
                name: "AssistantLeaveRequests");
        }
    }
}
