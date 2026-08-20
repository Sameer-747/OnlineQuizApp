using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OnlineQuizApp.Data;

#nullable disable

namespace OnlineQuizApp.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260820000000_AddQuizAttemptIntegrityFields")]
    /// <inheritdoc />
    public partial class AddQuizAttemptIntegrityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TabSwitchCount",
                table: "QuizAttempts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "AutoSubmitted",
                table: "QuizAttempts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TabSwitchCount",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "AutoSubmitted",
                table: "QuizAttempts");
        }
    }
}
