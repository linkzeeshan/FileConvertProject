using FileConvertPro.Data;
using FileConvertPro.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FileConvertPro
{
    public class ResetSuperAdmin
    {
        public static async Task Main(string[] args)
        {
            // Create a service collection and configure our dependencies
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(builder => 
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            
            // Add DbContext
            var connectionString = "Server=(localdb)\\mssqllocaldb;Database=FileConvertPro;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
                
            // Add Identity
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
                
            // Build the service provider
            var serviceProvider = services.BuildServiceProvider();
            
            // Get the required services
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var logger = serviceProvider.GetRequiredService<ILogger<ResetSuperAdmin>>();
            
            // Reset or create the super admin
            try
            {
                string superAdminEmail = "superadmin@fileconvertpro.com";
                string newPassword = "Admin@123";
                
                logger.LogInformation("Looking for super admin user...");
                var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);
                
                if (superAdmin != null)
                {
                    logger.LogInformation("Super admin found. Resetting password...");
                    
                    // Remove the current password
                    var removePasswordResult = await userManager.RemovePasswordAsync(superAdmin);
                    if (!removePasswordResult.Succeeded)
                    {
                        foreach (var error in removePasswordResult.Errors)
                        {
                            logger.LogError($"Error removing password: {error.Description}");
                        }
                        return;
                    }
                    
                    // Add the new password
                    var addPasswordResult = await userManager.AddPasswordAsync(superAdmin, newPassword);
                    if (addPasswordResult.Succeeded)
                    {
                        logger.LogInformation($"Password reset successful for {superAdminEmail}. New password: {newPassword}");
                    }
                    else
                    {
                        foreach (var error in addPasswordResult.Errors)
                        {
                            logger.LogError($"Error adding password: {error.Description}");
                        }
                    }
                }
                else
                {
                    logger.LogInformation("Super admin not found. Creating new super admin user...");
                    
                    // Create a new super admin user
                    var newSuperAdmin = new ApplicationUser
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
                    
                    var result = await userManager.CreateAsync(newSuperAdmin, newPassword);
                    if (result.Succeeded)
                    {
                        logger.LogInformation($"Super admin user created successfully. Email: {superAdminEmail}, Password: {newPassword}");
                        
                        // Add to SuperAdmin role
                        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                        if (!await roleManager.RoleExistsAsync("SuperAdmin"))
                        {
                            await roleManager.CreateAsync(new IdentityRole("SuperAdmin"));
                            logger.LogInformation("SuperAdmin role created.");
                        }
                        
                        await userManager.AddToRoleAsync(newSuperAdmin, "SuperAdmin");
                        logger.LogInformation("User added to SuperAdmin role.");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            logger.LogError($"Error creating user: {error.Description}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while resetting the super admin user.");
            }
            
            logger.LogInformation("Process completed. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
