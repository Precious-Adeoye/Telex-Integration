using Microsoft.EntityFrameworkCore;
using Telex_Integration.Models;

namespace Telex_Integration.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<Reminder> Reminders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.IsCompleted);
                entity.HasIndex(e => e.DueDate);
                entity.HasIndex(e => e.TelexChannelId);
            });

            modelBuilder.Entity<Reminder>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.IsSent, e.ReminderTime });

                entity.HasOne(e => e.Task)
                      .WithMany()
                      .HasForeignKey(e => e.TaskId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }    
}
