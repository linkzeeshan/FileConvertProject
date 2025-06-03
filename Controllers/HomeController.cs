using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FileConvertPro.Models;
using Microsoft.AspNetCore.Authorization;

namespace FileConvertPro.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        // Pass information about supported conversion types to the view
        ViewBag.ConversionTypes = new List<ConversionTypeInfo>
        {
            new ConversionTypeInfo { Type = ConversionType.MDBToCSV, Name = "MDB to CSV", Description = "Convert Microsoft Access Database files to CSV format" },
            new ConversionTypeInfo { Type = ConversionType.MdvToPdf, Name = "MDV to PDF", Description = "Convert MDV files to PDF format" },
            new ConversionTypeInfo { Type = ConversionType.PdfToDocx, Name = "PDF to Word", Description = "Convert PDF documents to editable Word documents" },
            new ConversionTypeInfo { Type = ConversionType.JpgToPng, Name = "JPG to PNG", Description = "Convert JPG images to PNG format" },
            new ConversionTypeInfo { Type = ConversionType.PngToJpg, Name = "PNG to JPG", Description = "Convert PNG images to JPG format" },
            new ConversionTypeInfo { Type = ConversionType.JpgToWebp, Name = "JPG to WebP", Description = "Convert JPG images to WebP format" },
            new ConversionTypeInfo { Type = ConversionType.PngToWebp, Name = "PNG to WebP", Description = "Convert PNG images to WebP format" },
            new ConversionTypeInfo { Type = ConversionType.WebpToJpg, Name = "WebP to JPG", Description = "Convert WebP images to JPG format" },
            new ConversionTypeInfo { Type = ConversionType.WebpToPng, Name = "WebP to PNG", Description = "Convert WebP images to PNG format" }
        };
        
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }
    
    public IActionResult About()
    {
        return View();
    }
    
    public IActionResult Contact()
    {
        return View();
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Contact(ContactFormModel model)
    {
        if (ModelState.IsValid)
        {
            // TODO: Implement email sending functionality
            // For now, we'll just redirect with a success message
            
            TempData["SuccessMessage"] = "Thank you for your message! We will get back to you shortly.";
            return RedirectToAction(nameof(Contact));
        }
        
        return View(model);
    }
    
    public IActionResult FAQ()
    {
        return View();
    }
    
    public IActionResult Features()
    {
        return View();
    }
    
    public IActionResult Pricing()
    {
        return View();
    }
    
    public IActionResult Terms()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
