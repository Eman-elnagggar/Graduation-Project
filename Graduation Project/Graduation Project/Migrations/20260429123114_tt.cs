using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graduation_Project.Migrations
{
    /// <inheritdoc />
    public partial class tt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RBC_Count",
                table: "CBC_Tests");

            migrationBuilder.DropColumn(
                name: "WBC_Count",
                table: "CBC_Tests");

            migrationBuilder.RenameColumn(
                name: "Platelet_Count",
                table: "CBC_Tests",
                newName: "platelet_count");

            migrationBuilder.RenameColumn(
                name: "Lymphocytes",
                table: "CBC_Tests",
                newName: "lymphocytes");

            migrationBuilder.AlterColumn<float>(
                name: "platelet_count",
                table: "CBC_Tests",
                type: "real",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<float>(
                name: "MCV",
                table: "CBC_Tests",
                type: "real",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<float>(
                name: "MCHC",
                table: "CBC_Tests",
                type: "real",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<float>(
                name: "MCH",
                table: "CBC_Tests",
                type: "real",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<float>(
                name: "lymphocytes",
                table: "CBC_Tests",
                type: "real",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<float>(
                name: "HB",
                table: "CBC_Tests",
                type: "real",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AddColumn<float>(
                name: "RBCs_Count",
                table: "CBC_Tests",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "WBC",
                table: "CBC_Tests",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RBCs_Count",
                table: "CBC_Tests");

            migrationBuilder.DropColumn(
                name: "WBC",
                table: "CBC_Tests");

            migrationBuilder.RenameColumn(
                name: "platelet_count",
                table: "CBC_Tests",
                newName: "Platelet_Count");

            migrationBuilder.RenameColumn(
                name: "lymphocytes",
                table: "CBC_Tests",
                newName: "Lymphocytes");

            migrationBuilder.AlterColumn<double>(
                name: "Platelet_Count",
                table: "CBC_Tests",
                type: "float",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<double>(
                name: "Lymphocytes",
                table: "CBC_Tests",
                type: "float",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<double>(
                name: "MCV",
                table: "CBC_Tests",
                type: "float",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<double>(
                name: "MCHC",
                table: "CBC_Tests",
                type: "float",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<double>(
                name: "MCH",
                table: "CBC_Tests",
                type: "float",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<double>(
                name: "HB",
                table: "CBC_Tests",
                type: "float",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AddColumn<double>(
                name: "RBC_Count",
                table: "CBC_Tests",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "WBC_Count",
                table: "CBC_Tests",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
