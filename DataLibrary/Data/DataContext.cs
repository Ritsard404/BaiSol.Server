using DataLibrary.Models;
using DataLibrary.Models.Gantt;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DataLibrary.Data
{
    public class DataContext(DbContextOptions<DataContext> options) : IdentityDbContext<AppUsers>(options)
    {
        public DbSet<Client> Client { get; set; }
        public DbSet<Installer> Installer { get; set; }
        public DbSet<Project> Project { get; set; }
        public DbSet<Supply> Supply { get; set; }
        public DbSet<Equipment> Equipment { get; set; }
        public DbSet<Material> Material { get; set; }
        public DbSet<Labor> Labor { get; set; }
        public DbSet<ProjectWorkLog> ProjectWorkLog { get; set; }
        public DbSet<UserLogs> UserLogs { get; set; }
        public DbSet<Requisition> Requisition { get; set; }
        public DbSet<GanttData> GanttData { get; set; }
        public DbSet<TaskProof> TaskProof { get; set; }
        public DbSet<Payment> Payment { get; set; }
        public DbSet<Notification> Notification { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Make sure to call the base method


            modelBuilder.Entity<GanttData>(entity =>
            {
                entity.HasKey(e => e.Id); // Set Id as the primary key
                // Configure the one-to-many relationship using only the foreign key
                entity.HasOne<Project>() // Specify the related entity type
                      .WithMany(p => p.GanttData) // A Project has many GanttData
                      .HasForeignKey(g => g.ProjId); // Specify the foreign key property


            });
        }

    }
}
