using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineQuizApp.Migrations
{
    /// <inheritdoc />
    public partial class ScopeCategoriesToSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SectionId",
                table: "Categories",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_SectionId",
                table: "Categories",
                column: "SectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Sections_SectionId",
                table: "Categories",
                column: "SectionId",
                principalTable: "Sections",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Sections_SectionId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_SectionId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "SectionId",
                table: "Categories");
        }
    }
}
