using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace FileConvertPro.Models
{
    public class FileConversionViewModel
    {
        [Required(ErrorMessage = "Please select a file to convert")]
        public IFormFile File { get; set; }
        
        [Required(ErrorMessage = "Please select a conversion type")]
        public ConversionType ConversionType { get; set; }
    }
}
