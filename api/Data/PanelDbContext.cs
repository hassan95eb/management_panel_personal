using ManagementPanel.Api.Areas;
using ManagementPanel.Api.Projects;
using Microsoft.EntityFrameworkCore;

namespace ManagementPanel.Api.Data;

public sealed class PanelDbContext(DbContextOptions<PanelDbContext> options) : DbContext(options)
{
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Project> Projects => Set<Project>();

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
        projects.Property(project => project.AreaKey).HasMaxLength(50).IsRequired();
        projects.HasOne<Area>()
            .WithMany()
            .HasForeignKey(project => project.AreaKey)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
