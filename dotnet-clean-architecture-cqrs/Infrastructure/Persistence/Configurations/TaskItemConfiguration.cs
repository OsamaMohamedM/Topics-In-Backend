using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("TaskItems");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);
        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.Priority)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(t => t.CreatedAt)
            .IsRequired();
        builder.HasIndex(t => t.ProjectId)
            .HasDatabaseName("IX_TaskItems_ProjectId");
    }
}