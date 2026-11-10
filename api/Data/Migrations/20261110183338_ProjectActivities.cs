using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManagementPanel.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProjectActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_project_tasks_ProjectId",
                table: "project_tasks");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_project_tasks_ProjectId_Id",
                table: "project_tasks",
                columns: new[] { "ProjectId", "Id" });

            migrationBuilder.CreateTable(
                name: "project_activities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    Report = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Link = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ActorId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_activities_project_tasks_ProjectId_TaskId",
                        columns: x => new { x.ProjectId, x.TaskId },
                        principalTable: "project_tasks",
                        principalColumns: new[] { "ProjectId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_project_activities_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_project_activities_ProjectId_TaskId",
                table: "project_activities",
                columns: new[] { "ProjectId", "TaskId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "project_activities");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_project_tasks_ProjectId_Id",
                table: "project_tasks");

            migrationBuilder.CreateIndex(
                name: "IX_project_tasks_ProjectId",
                table: "project_tasks",
                column: "ProjectId");
        }
    }
}
