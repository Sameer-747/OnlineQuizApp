using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using OnlineQuizApp.Data;

#nullable disable

namespace OnlineQuizApp.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260912000000_AddGlobalTestEvents")]
    /// <inheritdoc />
    public partial class AddGlobalTestEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A super-admin "global" event has no single section, so TestEvents.SectionId must
            // become nullable. Drop the existing Restrict FK first, alter the column, then
            // recreate the FK as optional.
            migrationBuilder.DropForeignKey(
                name: "FK_TestEvents_Sections_SectionId",
                table: "TestEvents");

            migrationBuilder.AlterColumn<int>(
                name: "SectionId",
                table: "TestEvents",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_TestEvents_Sections_SectionId",
                table: "TestEvents",
                column: "SectionId",
                principalTable: "Sections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.CreateTable(
                name: "TestEventSectionLanguages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TestEventId = table.Column<int>(type: "integer", nullable: false),
                    SectionId = table.Column<int>(type: "integer", nullable: false),
                    Language = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    QuizId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestEventSectionLanguages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestEventSectionLanguages_TestEvents_TestEventId",
                        column: x => x.TestEventId,
                        principalTable: "TestEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestEventSectionLanguages_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestEventSectionLanguages_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestEventSectionLanguages_TestEventId_SectionId",
                table: "TestEventSectionLanguages",
                columns: new[] { "TestEventId", "SectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestEventSectionLanguages_SectionId",
                table: "TestEventSectionLanguages",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_TestEventSectionLanguages_QuizId",
                table: "TestEventSectionLanguages",
                column: "QuizId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestEventSectionLanguages");

            migrationBuilder.DropForeignKey(
                name: "FK_TestEvents_Sections_SectionId",
                table: "TestEvents");

            migrationBuilder.AlterColumn<int>(
                name: "SectionId",
                table: "TestEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TestEvents_Sections_SectionId",
                table: "TestEvents",
                column: "SectionId",
                principalTable: "Sections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
