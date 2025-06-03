using FileConvertPro.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FileConvertPro.Services
{
    public interface IAccountService
    {
        Task<bool> IsSubscriptionValidAsync(string userId);
        Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password, string role);
        Task<bool> AssignRoleAsync(ApplicationUser user, string role);
        Task<bool> IsInRoleAsync(ApplicationUser user, string role);
        Task<IList<string>> GetUserRolesAsync(ApplicationUser user);
        Task<bool> RemoveFromRoleAsync(ApplicationUser user, string role);
        Task<bool> UpdateSubscriptionAsync(string userId, SubscriptionTier tier, DateTime endDate);
        Task<ApplicationUser> GetUserByIdAsync(string userId);
        Task<ApplicationUser> GetUserByEmailAsync(string email);
        Task<IEnumerable<ApplicationUser>> GetRecentUsersAsync(int count);
        Task<int> GetActiveSubscribersCountAsync();
    }
}
