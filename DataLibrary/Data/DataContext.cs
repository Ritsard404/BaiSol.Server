using DataLibrary.Models;
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Make sure to call the base method

        }

    }
}
