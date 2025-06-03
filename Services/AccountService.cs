using FileConvertPro.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileConvertPro.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<bool> IsSubscriptionValidAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            // Free tier is always valid
            if (user.SubscriptionTier == SubscriptionTier.Free)
                return true;

            // Check if subscription is still valid
            return user.SubscriptionEndDate.HasValue && user.SubscriptionEndDate.Value > DateTime.UtcNow;
        }

        public async Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password, string role)
        {
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                // Ensure the role exists
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }

                // Assign the role to the user
                await _userManager.AddToRoleAsync(user, role);
            }

            return result;
        }

        public async Task<bool> AssignRoleAsync(ApplicationUser user, string role)
        {
            // Ensure the role exists
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }

            // Add the user to the role
            var result = await _userManager.AddToRoleAsync(user, role);
            return result.Succeeded;
        }

        public async Task<bool> IsInRoleAsync(ApplicationUser user, string role)
        {
            return await _userManager.IsInRoleAsync(user, role);
        }

        public async Task<IList<string>> GetUserRolesAsync(ApplicationUser user)
        {
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<bool> RemoveFromRoleAsync(ApplicationUser user, string role)
        {
            var result = await _userManager.RemoveFromRoleAsync(user, role);
            return result.Succeeded;
        }

        public async Task<bool> UpdateSubscriptionAsync(string userId, SubscriptionTier tier, DateTime endDate)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            user.SubscriptionTier = tier;
            user.SubscriptionEndDate = endDate;

            // If user subscribes to a paid tier, assign Admin role
            if (tier != SubscriptionTier.Free)
            {
                await AssignRoleAsync(user, "Admin");
            }
            // If downgrading to free, remove Admin role
            else
            {
                if (await IsInRoleAsync(user, "Admin"))
                {
                    await RemoveFromRoleAsync(user, "Admin");
                }
            }

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<ApplicationUser> GetUserByIdAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId);
        }

        public async Task<ApplicationUser> GetUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<IEnumerable<ApplicationUser>> GetRecentUsersAsync(int count)
        {
            return await _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<int> GetActiveSubscribersCountAsync()
        {
            return await _userManager.Users
                .Where(u => u.SubscriptionTier != SubscriptionTier.Free && 
                       u.SubscriptionEndDate.HasValue && 
                       u.SubscriptionEndDate.Value > DateTime.UtcNow)
                .CountAsync();
        }
    }
}
