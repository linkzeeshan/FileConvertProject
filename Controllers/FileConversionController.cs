using FileConvertPro.Models;
using FileConvertPro.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FileConvertPro.Controllers
{
    [Authorize]
    public class FileConversionController : Controller
    {
        private readonly IFileConversionService _conversionService;
        private readonly ILogger<FileConversionController> _logger;

        public FileConversionController(
            IFileConversionService conversionService,
            ILogger<FileConversionController> logger)
        {
            _conversionService = conversionService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var conversions = await _conversionService.GetUserConversionsAsync(userId);
                return View(conversions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user conversions");
                TempData["ErrorMessage"] = "An error occurred while retrieving your conversions.";
                return RedirectToAction("Index", "Home");
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ConversionType conversionType, Microsoft.AspNetCore.Http.IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("file", "Please select a file to convert.");
                return View();
            }

            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var conversionId = await _conversionService.ConvertFileAsync(file, conversionType, userId);
                
                TempData["SuccessMessage"] = "Your file has been uploaded and is being processed.";
                return RedirectToAction(nameof(Details), new { id = conversionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during file conversion: {Message}", ex.Message);
                
                // Provide more detailed error message to help diagnose the issue
                string errorMessage = "An error occurred during file conversion: ";
                
                if (ex.Message.Contains("Microsoft.ACE.OLEDB") || ex.Message.Contains("Microsoft.Jet.OLEDB"))
                {
                    errorMessage += "The Microsoft Access Database Engine is not installed. Please install the Access Database Engine from Microsoft's website.";
                }
                else if (ex.Message.Contains("Could not find file"))
                {
                    errorMessage += "The file could not be found or accessed.";
                }
                else if (ex.Message.Contains("No tables found"))
                {
                    errorMessage += "No tables were found in the MDB file. The file may be corrupted or not a valid Access database.";
                }
                else if (ex.Message.Contains("Not a valid file format") || ex.Message.Contains("invalid format"))
                {
                    errorMessage += "The file is not in a valid format for the selected conversion type.";
                }
                else
                {
                    // For security, don't expose the full exception message to users in production
                    #if DEBUG
                    errorMessage += ex.Message;
                    #else
                    errorMessage += "Please check the file and try again. If the problem persists, contact support.";
                    #endif
                }
                
                ModelState.AddModelError("", errorMessage);
                return View();
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var conversion = await _conversionService.GetConversionByIdAsync(id, userId);
                
                if (conversion == null)
                {
                    return NotFound();
                }

                return View(conversion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving conversion details for ID {id}");
                TempData["ErrorMessage"] = "An error occurred while retrieving conversion details.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Download(int id)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var conversion = await _conversionService.GetConversionByIdAsync(id, userId);
                
                if (conversion == null || conversion.Status != ConversionStatus.Completed || string.IsNullOrEmpty(conversion.ConvertedFilePath))
                {
                    return NotFound();
                }

                var fileBytes = System.IO.File.ReadAllBytes(conversion.ConvertedFilePath);
                return File(fileBytes, "application/octet-stream", conversion.ConvertedFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading conversion file for ID {id}");
                TempData["ErrorMessage"] = "An error occurred while downloading the file.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
    }
}
