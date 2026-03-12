using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HinataProject.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveRightsFromRoleToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "rights",
                table: "roles");

            migrationBuilder.AddColumn<string>(
                name: "rights",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "rights",
                table: "users");

            migrationBuilder.AddColumn<string>(
                name: "rights",
                table: "roles",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
