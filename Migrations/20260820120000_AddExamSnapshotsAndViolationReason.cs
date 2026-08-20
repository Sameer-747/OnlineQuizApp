using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using OnlineQuizApp.Data;

#nullable disable

namespace OnlineQuizApp.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260820120000_AddExamSnapshotsAndViolationReason")]
    /// <inheritdoc />
    public partial class AddExamSnapshotsAndViolationReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ViolationReason",
                table: "QuizAttempts",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExamSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    QuizId = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ImageData = table.Column<string>(type: "text", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamSnapshots_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamSnapshots_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamSnapshots_UserId",
                table: "ExamSnapshots",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSnapshots_QuizId",
                table: "ExamSnapshots",
                column: "QuizId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamSnapshots");

            migrationBuilder.DropColumn(
                name: "ViolationReason",
                table: "QuizAttempts");
        }
    }
}
