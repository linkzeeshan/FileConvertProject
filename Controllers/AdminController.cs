using FileConvertPro.Data;
using FileConvertPro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using FileConvertPro.Services;

namespace FileConvertPro.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdminController> _logger;
        private readonly IFileConversionService _fileConversionService;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<AdminController> logger,
            IFileConversionService fileConversionService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _fileConversionService = fileConversionService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Get summary statistics
                var stats = new AdminDashboardViewModel
                {
                    TotalUsers = await _context.Users.CountAsync(),
                    TotalConversions = await _context.FileConversions.CountAsync(),
                    PendingConversions = await _context.FileConversions
                        .CountAsync(c => c.Status == ConversionStatus.Pending || c.Status == ConversionStatus.Processing),
                    FailedConversions = await _context.FileConversions
                        .CountAsync(c => c.Status == ConversionStatus.Failed),
                    RecentConversions = await _context.FileConversions
                        .Include(c => c.User)
                        .OrderByDescending(c => c.CreatedAt)
                        .Take(10)
                        .ToListAsync()
                };

                return View(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin dashboard data");
                TempData["ErrorMessage"] = "An error occurred while retrieving dashboard data.";
                return View(new AdminDashboardViewModel());
            }
        }

        public async Task<IActionResult> Users()
        {
            try
            {
                var users = await _context.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync();

                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users list");
                TempData["ErrorMessage"] = "An error occurred while retrieving users.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> UserDetails(string id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.FileConversions)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return NotFound();
                }

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user details for ID {id}");
                TempData["ErrorMessage"] = "An error occurred while retrieving user details.";
                return RedirectToAction(nameof(Users));
            }
        }

        public async Task<IActionResult> Conversions()
        {
            try
            {
                var conversions = await _context.FileConversions
                    .Include(c => c.User)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                return View(conversions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conversions list");
                TempData["ErrorMessage"] = "An error occurred while retrieving conversions.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSubscription(string userId, SubscriptionTier tier, DateTime? endDate)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound();
                }

                user.SubscriptionTier = tier;
                user.SubscriptionEndDate = endDate;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "User subscription updated successfully.";
                return RedirectToAction(nameof(UserDetails), new { id = userId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating subscription for user ID {userId}");
                TempData["ErrorMessage"] = "An error occurred while updating the subscription.";
                return RedirectToAction(nameof(UserDetails), new { id = userId });
            }
        }

        public async Task<IActionResult> DownloadConvertedFile(int id)
        {
            try
            {
                var conversion = await _fileConversionService.GetConversionByIdForAdminAsync(id);

                if (conversion == null)
                {
                    _logger.LogWarning("Admin download attempt: Conversion with ID {ConversionId} not found.", id);
                    TempData["ErrorMessage"] = "Conversion record not found.";
                    return RedirectToAction(nameof(Conversions));
                }

                if (conversion.Status != ConversionStatus.Completed || string.IsNullOrEmpty(conversion.ConvertedFilePath))
                {
                    _logger.LogWarning("Admin download attempt: Conversion ID {ConversionId} is not completed or has no file path. Status: {Status}", id, conversion.Status);
                    TempData["ErrorMessage"] = "File is not available for download (not completed or path missing).";
                    return RedirectToAction(nameof(Conversions));
                }

                if (!System.IO.File.Exists(conversion.ConvertedFilePath))
                {
                    _logger.LogError("Admin download attempt: File not found at path {FilePath} for conversion ID {ConversionId}", conversion.ConvertedFilePath, id);
                    TempData["ErrorMessage"] = "Converted file not found on server. It might have been moved or deleted.";
                    return RedirectToAction(nameof(Conversions));
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(conversion.ConvertedFilePath);
                return File(fileBytes, "application/octet-stream", conversion.ConvertedFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during admin download for conversion ID {ConversionId}", id);
                TempData["ErrorMessage"] = "An error occurred while trying to download the file.";
                return RedirectToAction(nameof(Conversions));
            }
        }
    }
}
