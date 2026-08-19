using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OnlineQuizApp.Migrations
{
    /// <inheritdoc />
    public partial class AddTestEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TestEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SectionId = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestEvents_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TestEvents_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddColumn<int>(
                name: "TestEventId",
                table: "Quizzes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TestEventAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TestEventId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    QuizId = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestEventAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestEventAssignments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestEventAssignments_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestEventAssignments_TestEvents_TestEventId",
                        column: x => x.TestEventId,
                        principalTable: "TestEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_TestEventId",
                table: "Quizzes",
                column: "TestEventId");

            migrationBuilder.CreateIndex(
                name: "IX_TestEvents_CreatedByUserId",
                table: "TestEvents",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TestEvents_SectionId",
                table: "TestEvents",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_TestEventAssignments_QuizId",
                table: "TestEventAssignments",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_TestEventAssignments_TestEventId_UserId",
                table: "TestEventAssignments",
                columns: new[] { "TestEventId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestEventAssignments_UserId",
                table: "TestEventAssignments",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_TestEvents_TestEventId",
                table: "Quizzes",
                column: "TestEventId",
                principalTable: "TestEvents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_TestEvents_TestEventId",
                table: "Quizzes");

            migrationBuilder.DropTable(
                name: "TestEventAssignments");

            migrationBuilder.DropTable(
                name: "TestEvents");

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_TestEventId",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "TestEventId",
                table: "Quizzes");
        }
    }
}
