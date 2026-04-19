using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graduation_Project.Migrations
{
    /// <inheritdoc />
    public partial class ReuseSlotsWithBookingHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_AppointmentID",
                table: "Bookings");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Bookings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_AppointmentID",
                table: "Bookings",
                column: "AppointmentID",
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_AppointmentID",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_AppointmentID",
                table: "Bookings",
                column: "AppointmentID",
                unique: true);
        }
    }
}
