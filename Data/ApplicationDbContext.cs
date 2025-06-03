using FileConvertPro.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FileConvertPro.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<FileConversion> FileConversions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure relationships and constraints
            builder.Entity<FileConversion>()
                .HasOne(fc => fc.User)
                .WithMany(u => u.FileConversions)
                .HasForeignKey(fc => fc.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
