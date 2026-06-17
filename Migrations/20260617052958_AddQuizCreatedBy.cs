using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineQuizApp.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizCreatedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Quizzes",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_CreatedByUserId",
                table: "Quizzes",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_AspNetUsers_CreatedByUserId",
                table: "Quizzes",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_AspNetUsers_CreatedByUserId",
                table: "Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_CreatedByUserId",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Quizzes");
        }
    }
}
