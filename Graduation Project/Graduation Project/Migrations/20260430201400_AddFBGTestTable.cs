using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graduation_Project.Migrations
{
    /// <inheritdoc />
    public partial class AddFBGTestTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FBG_Tests",
                columns: table => new
                {
                    LabTestID = table.Column<int>(type: "int", nullable: false),
                    FBG = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FBG_Tests", x => x.LabTestID);
                    table.ForeignKey(
                        name: "FK_FBG_Tests_LabTests_LabTestID",
                        column: x => x.LabTestID,
                        principalTable: "LabTests",
                        principalColumn: "LabTestID",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FBG_Tests");
        }
    }
}
