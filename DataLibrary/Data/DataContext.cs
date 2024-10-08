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



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Make sure to call the base method

        }

    }
}
