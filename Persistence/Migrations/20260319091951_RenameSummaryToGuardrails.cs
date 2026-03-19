using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HinataProject.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameSummaryToGuardrails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "summary",
                table: "nodes",
                newName: "guardrails");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "guardrails",
                table: "nodes",
                newName: "summary");
        }
    }
}
