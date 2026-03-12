using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace HinataProject.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    rights = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    last_modified_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    color = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    last_modified_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    subject = table.Column<string>(type: "text", nullable: false),
                    info = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    last_modified_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    last_modified_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_log_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_name = table.Column<string>(type: "text", nullable: false),
                    old_value = table.Column<string>(type: "text", nullable: true),
                    new_value = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    last_modified_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_log_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_log_entries_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    last_modified_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comments", x => x.id);
                    table.ForeignKey(
                        name: "fk_comments_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "node_added_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_node_added_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_node_added_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "node_added_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_node_added_types", x => x.id);
                    table.ForeignKey(
                        name: "fk_node_added_types_types_type_id",
                        column: x => x.type_id,
                        principalTable: "types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "node_added_workflows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_node_added_workflows", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "node_assignees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    last_modified_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_node_assignees", x => x.id);
                    table.ForeignKey(
                        name: "fk_node_assignees_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_node_assignees_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "node_removed_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_node_removed_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_node_removed_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "node_removed_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_node_removed_types", x => x.id);
                    table.ForeignKey(
                        name: "fk_node_removed_types_types_type_id",
                        column: x => x.type_id,
                        principalTable: "types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "node_removed_workflows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_node_removed_workflows", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "nodes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    public_id = table.Column<int>(type: "integer", nullable: false),
                    manifest = table.Column<string>(type: "text", nullable: false),
                    caption = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    summary = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    last_modified_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    workflow_id = table.Column<Guid>(type: "uuid", nullable: true),
                    state_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nodes", x => x.id);
                    table.ForeignKey(
                        name: "fk_nodes_nodes_parent_id",
                        column: x => x.parent_id,
                        principalTable: "nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_nodes_types_type_id",
                        column: x => x.type_id,
                        principalTable: "types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "role_states",
                columns: table => new
                {
                    roles_id = table.Column<Guid>(type: "uuid", nullable: false),
                    states_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_states", x => new { x.roles_id, x.states_id });
                    table.ForeignKey(
                        name: "fk_role_states_roles_roles_id",
                        column: x => x.roles_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    is_final_success = table.Column<bool>(type: "boolean", nullable: false),
                    is_final_failure = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    last_modified_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    workflow_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_states", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workflows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    last_modified_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    default_state_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflows", x => x.id);
                    table.ForeignKey(
                        name: "fk_workflows_states_default_state_id",
                        column: x => x.default_state_id,
                        principalTable: "states",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

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
                name: "ix_audit_log_entries_node_id",
                table: "audit_log_entries",
                column: "node_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_entries_user_id",
                table: "audit_log_entries",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_comments_node_id",
                table: "comments",
                column: "node_id");

            migrationBuilder.CreateIndex(
                name: "ix_comments_user_id",
                table: "comments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_node_added_roles_node_id",
                table: "node_added_roles",
                column: "node_id");

            migrationBuilder.CreateIndex(
                name: "ix_node_added_roles_role_id",
                table: "node_added_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_node_added_types_node_id",
                table: "node_added_types",
                column: "node_id");

            migrationBuilder.CreateIndex(
                name: "ix_node_added_types_type_id",
                table: "node_added_types",
                column: "type_id");

            migrationBuilder.CreateIndex(
                name: "ix_node_added_workflows_node_id",
                table: "node_added_workflows",
                column: "node_id");

            migrationBuilder.CreateIndex(
                name: "ix_node_added_workflows_workflow_id",
                table: "node_added_workflows",
                column: "workflow_id");

            migrationBuilder.CreateIndex(
                name: "ix_node_assignees_node_id_role_id",
                table: "node_assignees",
                columns: new[] { "node_id", "role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_node_assignees_role_id",
                table: "node_assignees",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_node_assignees_user_id",
                table: "node_assignees",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_node_removed_roles_node_id",
                table: "node_removed_roles",
                column: "node_id");

            migrationBuilder.CreateIndex(
                name: "ix_node_removed_roles_role_id",
                table: "node_removed_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_node_removed_types_node_id",
                table: "node_removed_types",
                column: "node_id");

            migrationBuilder.CreateIndex(
                name: "ix_node_removed_types_type_id",
                table: "node_removed_types",
                column: "type_id");

            migrationBuilder.CreateIndex(
                name: "ix_node_removed_workflows_node_id",
                table: "node_removed_workflows",
                column: "node_id");

            migrationBuilder.CreateIndex(
                name: "ix_node_removed_workflows_workflow_id",
                table: "node_removed_workflows",
                column: "workflow_id");

            migrationBuilder.CreateIndex(
                name: "ix_nodes_parent_id",
                table: "nodes",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_nodes_state_id",
                table: "nodes",
                column: "state_id");

            migrationBuilder.CreateIndex(
                name: "ix_nodes_type_id",
                table: "nodes",
                column: "type_id");

            migrationBuilder.CreateIndex(
                name: "ix_nodes_workflow_id",
                table: "nodes",
                column: "workflow_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_states_states_id",
                table: "role_states",
                column: "states_id");

            migrationBuilder.CreateIndex(
                name: "ix_states_workflow_id",
                table: "states",
                column: "workflow_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_user_id",
                table: "user_roles",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_default_for_types_default_for_types_workflows_id",
                table: "workflow_default_for_types",
                column: "default_for_types_workflows_id");

            migrationBuilder.CreateIndex(
                name: "ix_workflows_default_state_id",
                table: "workflows",
                column: "default_state_id");

            migrationBuilder.AddForeignKey(
                name: "fk_audit_log_entries_nodes_node_id",
                table: "audit_log_entries",
                column: "node_id",
                principalTable: "nodes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_comments_nodes_node_id",
                table: "comments",
                column: "node_id",
                principalTable: "nodes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_node_added_roles_nodes_node_id",
                table: "node_added_roles",
                column: "node_id",
                principalTable: "nodes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_node_added_types_nodes_node_id",
                table: "node_added_types",
                column: "node_id",
                principalTable: "nodes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_node_added_workflows_nodes_node_id",
                table: "node_added_workflows",
                column: "node_id",
                principalTable: "nodes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_node_added_workflows_workflows_workflow_id",
                table: "node_added_workflows",
                column: "workflow_id",
                principalTable: "workflows",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_node_assignees_nodes_node_id",
                table: "node_assignees",
                column: "node_id",
                principalTable: "nodes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_node_removed_roles_nodes_node_id",
                table: "node_removed_roles",
                column: "node_id",
                principalTable: "nodes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_node_removed_types_nodes_node_id",
                table: "node_removed_types",
                column: "node_id",
                principalTable: "nodes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_node_removed_workflows_nodes_node_id",
                table: "node_removed_workflows",
                column: "node_id",
                principalTable: "nodes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_node_removed_workflows_workflows_workflow_id",
                table: "node_removed_workflows",
                column: "workflow_id",
                principalTable: "workflows",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_nodes_states_state_id",
                table: "nodes",
                column: "state_id",
                principalTable: "states",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_nodes_workflows_workflow_id",
                table: "nodes",
                column: "workflow_id",
                principalTable: "workflows",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_role_states_states_states_id",
                table: "role_states",
                column: "states_id",
                principalTable: "states",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_states_workflows_workflow_id",
                table: "states",
                column: "workflow_id",
                principalTable: "workflows",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_states_workflows_workflow_id",
                table: "states");

            migrationBuilder.DropTable(
                name: "audit_log_entries");

            migrationBuilder.DropTable(
                name: "comments");

            migrationBuilder.DropTable(
                name: "node_added_roles");

            migrationBuilder.DropTable(
                name: "node_added_types");

            migrationBuilder.DropTable(
                name: "node_added_workflows");

            migrationBuilder.DropTable(
                name: "node_assignees");

            migrationBuilder.DropTable(
                name: "node_removed_roles");

            migrationBuilder.DropTable(
                name: "node_removed_types");

            migrationBuilder.DropTable(
                name: "node_removed_workflows");

            migrationBuilder.DropTable(
                name: "role_states");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "workflow_default_for_types");

            migrationBuilder.DropTable(
                name: "nodes");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "types");

            migrationBuilder.DropTable(
                name: "workflows");

            migrationBuilder.DropTable(
                name: "states");
        }
    }
}
