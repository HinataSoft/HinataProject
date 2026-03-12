using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HinataProject.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveDefaultWorkflowFromWorkflowToType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workflow_default_for_types");

            migrationBuilder.AddColumn<Guid>(
                name: "default_workflow_id",
                table: "types",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_types_default_workflow_id",
                table: "types",
                column: "default_workflow_id");

            migrationBuilder.AddForeignKey(
                name: "fk_types_workflows_default_workflow_id",
                table: "types",
                column: "default_workflow_id",
                principalTable: "workflows",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_types_workflows_default_workflow_id",
                table: "types");

            migrationBuilder.DropIndex(
                name: "ix_types_default_workflow_id",
                table: "types");

            migrationBuilder.DropColumn(
                name: "default_workflow_id",
                table: "types");

            migrationBuilder.CreateTable(
                name: "workflow_default_for_types",
                columns: table => new
                {
                    default_for_types_id = table.Column<Guid>(type: "uuid", nullable: false),
                    default_for_types_workflows_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_default_for_types", x => new { x.default_for_types_id, x.default_for_types_workflows_id });
                    table.ForeignKey(
                        name: "fk_workflow_default_for_types_types_default_for_types_id",
                        column: x => x.default_for_types_id,
                        principalTable: "types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_workflow_default_for_types_workflows_default_for_types_work",
                        column: x => x.default_for_types_workflows_id,
                        principalTable: "workflows",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_default_for_types_default_for_types_workflows_id",
                table: "workflow_default_for_types",
                column: "default_for_types_workflows_id");
        }
    }
}
