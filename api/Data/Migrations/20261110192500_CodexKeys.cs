using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManagementPanel.Api.Data.Migrations;

public partial class CodexKeys : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "codex_keys",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                SecretHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_codex_keys", x => x.Id));

        migrationBuilder.CreateTable(
            name: "codex_project_grants",
            columns: table => new
            {
                KeyId = table.Column<Guid>(type: "uuid", nullable: false),
                ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                CanRead = table.Column<bool>(type: "boolean", nullable: false),
                CanWriteActivity = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_codex_project_grants", x => new { x.KeyId, x.ProjectId });
                table.ForeignKey("FK_codex_project_grants_codex_keys_KeyId", x => x.KeyId, "codex_keys", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_codex_project_grants_projects_ProjectId", x => x.ProjectId, "projects", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_codex_keys_SecretHash", "codex_keys", "SecretHash", unique: true);
        migrationBuilder.CreateIndex("IX_codex_project_grants_ProjectId", "codex_project_grants", "ProjectId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("codex_project_grants");
        migrationBuilder.DropTable("codex_keys");
    }
}
