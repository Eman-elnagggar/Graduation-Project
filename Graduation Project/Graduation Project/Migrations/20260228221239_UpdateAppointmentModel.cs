using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graduation_Project.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAppointmentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "PatientID",
                table: "Appointments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "CreatedByAssistantID",
                table: "Appointments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_CreatedByAssistantID",
                table: "Appointments",
                column: "CreatedByAssistantID");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Assistants_CreatedByAssistantID",
                table: "Appointments",
                column: "CreatedByAssistantID",
                principalTable: "Assistants",
                principalColumn: "AssistantID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Assistants_CreatedByAssistantID",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_CreatedByAssistantID",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "CreatedByAssistantID",
                table: "Appointments");

            migrationBuilder.AlterColumn<int>(
                name: "PatientID",
                table: "Appointments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
