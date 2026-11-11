using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ManagementPanel.Api.Data.Migrations;

[DbContext(typeof(PanelDbContext))]
[Migration("20261111200000_AddInbox")]
public sealed class AddInbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE inbox_items (
                "Id" uuid PRIMARY KEY,
                "Text" varchar(4000) NOT NULL,
                "CreatedAt" timestamptz NOT NULL,
                "ProjectId" uuid REFERENCES projects ("Id") ON DELETE RESTRICT,
                "TaskId" uuid,
                CONSTRAINT "FK_inbox_items_project_tasks_ProjectId_TaskId"
                    FOREIGN KEY ("ProjectId", "TaskId") REFERENCES project_tasks ("ProjectId", "Id") ON DELETE RESTRICT
            );
            CREATE INDEX "IX_inbox_items_ProjectId_TaskId" ON inbox_items ("ProjectId", "TaskId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable("inbox_items");
}
