using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManagementPanel.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProjectDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArchivedAt",
                table: "projects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "projects",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnvironmentUrl",
                table: "projects",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NextStep",
                table: "projects",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepositoryUrl",
                table: "projects",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "projects",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "EnvironmentUrl",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "NextStep",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "RepositoryUrl",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "projects");
        }
    }
}
