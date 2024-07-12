using DataLibrary.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DataLibrary.Data
{
    public class DataContext(DbContextOptions<DataContext> options) : IdentityDbContext<AppUsers>(options)
    {
        public DbSet<Client> Client { get; set; }
        public DbSet<Installer> Installer { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Make sure to call the base method

        }

    }
}
