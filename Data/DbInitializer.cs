using FileConvertPro.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FileConvertPro.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager, 
            RoleManager<IdentityRole> roleManager,
            ILogger logger)
        {
            // Apply any pending migrations
            try
            {
                context.Database.Migrate();
                logger.LogInformation("Database migrations applied successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while applying database migrations.");
                throw;
            }

            // Seed roles
            string[] roles = { "SuperAdmin", "Admin", "Customer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    logger.LogInformation($"Created role: {role}");
                }
                else
                {
                    logger.LogInformation($"Role already exists: {role}");
                }
            }

            // Seed super admin user
            string superAdminEmail = "superadmin@fileconvertpro.com";
            string superAdminPassword = "Admin@123";
            
            // Check if super admin exists and delete it to recreate with correct password
            var existingSuperAdmin = await userManager.FindByEmailAsync(superAdminEmail);
            
            if (existingSuperAdmin != null)
            {
                logger.LogInformation("Found existing super admin user. Deleting to recreate with correct password...");
                await userManager.DeleteAsync(existingSuperAdmin);
                logger.LogInformation("Existing super admin user deleted.");
            }
            
            // Create a new super admin user
            logger.LogInformation("Creating super admin user...");
            var superAdmin = new ApplicationUser
            {
                UserName = superAdminEmail,
                Email = superAdminEmail,
                EmailConfirmed = true,
                FirstName = "Super",
                LastName = "Admin",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                SubscriptionTier = SubscriptionTier.Enterprise,
                SubscriptionEndDate = DateTime.UtcNow.AddYears(100) // Never expires
            };

            var result = await userManager.CreateAsync(superAdmin, superAdminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
                logger.LogInformation($"Super admin user created successfully with password: {superAdminPassword}");
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogError($"Failed to create super admin user. Errors: {errors}");
            }
        }
    }
}
