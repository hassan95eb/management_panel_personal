using ManagementPanel.Api.Areas;
using ManagementPanel.Api.Projects;
using ManagementPanel.Api.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ManagementPanel.Api.Data;

public sealed class PanelDbContext(DbContextOptions<PanelDbContext> options) : DbContext(options)
{
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var areas = modelBuilder.Entity<Area>();
        areas.ToTable("areas");
        areas.HasKey(area => area.Key);
        areas.Property(area => area.Key).HasMaxLength(50);
        areas.Property(area => area.Name).HasMaxLength(100).IsRequired();
        areas.HasData(new Area { Key = Area.ProjectsKey, Name = "پروژه‌ها" });

        var projects = modelBuilder.Entity<Project>();
        projects.ToTable("projects");
        projects.HasKey(project => project.Id);
        projects.Property(project => project.Name).HasMaxLength(200).IsRequired();
        projects.Property(project => project.Description).HasMaxLength(4000);
        projects.Property(project => project.Status).HasMaxLength(20).HasDefaultValue("active");
        projects.Property(project => project.RepositoryUrl).HasMaxLength(2048);
        projects.Property(project => project.EnvironmentUrl).HasMaxLength(2048);
        projects.Property(project => project.NextStep).HasMaxLength(1000);
        projects.Property(project => project.AreaKey).HasMaxLength(50).IsRequired();
        projects.HasOne<Area>()
            .WithMany()
            .HasForeignKey(project => project.AreaKey)
            .OnDelete(DeleteBehavior.Restrict);

        var tasks = modelBuilder.Entity<ProjectTask>();
        tasks.ToTable("project_tasks");
        tasks.HasKey(task => task.Id);
        tasks.Property(task => task.Title).HasMaxLength(200).IsRequired();
        tasks.Property(task => task.Status).HasMaxLength(20).IsRequired();
        tasks.Property(task => task.Priority).HasMaxLength(20).IsRequired();
        tasks.Property(task => task.BlockedReason).HasMaxLength(1000);
        tasks.HasOne<Project>()
            .WithMany()
            .HasForeignKey(task => task.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
