using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graduation_Project.Migrations
{
    /// <inheritdoc />
    public partial class AddWhenMeasureBloodSugar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // MeasurementTime columns were already added by
            // 20260221212538_AddMeasurementTime_ToBPAndBS.
            // This migration is intentionally left empty to avoid
            // a duplicate-column error when the database is updated.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to revert – see comment in Up().
        }
    }
}
