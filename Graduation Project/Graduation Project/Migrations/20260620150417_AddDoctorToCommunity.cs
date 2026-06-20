using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graduation_Project.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorToCommunity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CommunityLikes_CommunityPostId_PatientID",
                table: "CommunityLikes");

            migrationBuilder.AlterColumn<int>(
                name: "PatientID",
                table: "CommunityPosts",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "DoctorID",
                table: "CommunityPosts",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PatientID",
                table: "CommunityLikes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "DoctorID",
                table: "CommunityLikes",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PatientID",
                table: "CommunityComments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "DoctorID",
                table: "CommunityComments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunityPosts_DoctorID",
                table: "CommunityPosts",
                column: "DoctorID");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityLikes_CommunityPostId_DoctorID",
                table: "CommunityLikes",
                columns: new[] { "CommunityPostId", "DoctorID" },
                unique: true,
                filter: "[DoctorID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityLikes_CommunityPostId_PatientID",
                table: "CommunityLikes",
                columns: new[] { "CommunityPostId", "PatientID" },
                unique: true,
                filter: "[PatientID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityLikes_DoctorID",
                table: "CommunityLikes",
                column: "DoctorID");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityComments_DoctorID",
                table: "CommunityComments",
                column: "DoctorID");

            migrationBuilder.AddForeignKey(
                name: "FK_CommunityComments_Doctors_DoctorID",
                table: "CommunityComments",
                column: "DoctorID",
                principalTable: "Doctors",
                principalColumn: "DoctorID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CommunityLikes_Doctors_DoctorID",
                table: "CommunityLikes",
                column: "DoctorID",
                principalTable: "Doctors",
                principalColumn: "DoctorID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CommunityPosts_Doctors_DoctorID",
                table: "CommunityPosts",
                column: "DoctorID",
                principalTable: "Doctors",
                principalColumn: "DoctorID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommunityComments_Doctors_DoctorID",
                table: "CommunityComments");

            migrationBuilder.DropForeignKey(
                name: "FK_CommunityLikes_Doctors_DoctorID",
                table: "CommunityLikes");

            migrationBuilder.DropForeignKey(
                name: "FK_CommunityPosts_Doctors_DoctorID",
                table: "CommunityPosts");

            migrationBuilder.DropIndex(
                name: "IX_CommunityPosts_DoctorID",
                table: "CommunityPosts");

            migrationBuilder.DropIndex(
                name: "IX_CommunityLikes_CommunityPostId_DoctorID",
                table: "CommunityLikes");

            migrationBuilder.DropIndex(
                name: "IX_CommunityLikes_CommunityPostId_PatientID",
                table: "CommunityLikes");

            migrationBuilder.DropIndex(
                name: "IX_CommunityLikes_DoctorID",
                table: "CommunityLikes");

            migrationBuilder.DropIndex(
                name: "IX_CommunityComments_DoctorID",
                table: "CommunityComments");

            migrationBuilder.DropColumn(
                name: "DoctorID",
                table: "CommunityPosts");

            migrationBuilder.DropColumn(
                name: "DoctorID",
                table: "CommunityLikes");

            migrationBuilder.DropColumn(
                name: "DoctorID",
                table: "CommunityComments");

            migrationBuilder.AlterColumn<int>(
                name: "PatientID",
                table: "CommunityPosts",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PatientID",
                table: "CommunityLikes",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PatientID",
                table: "CommunityComments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunityLikes_CommunityPostId_PatientID",
                table: "CommunityLikes",
                columns: new[] { "CommunityPostId", "PatientID" },
                unique: true);
        }
    }
}
