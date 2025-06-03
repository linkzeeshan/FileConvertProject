using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileConvertPro.Models
{
    public class FileConversion
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        public string OriginalFileName { get; set; }
        
        [Required]
        public string OriginalFilePath { get; set; }
        
        public long OriginalFileSize { get; set; }
        
        public string ConvertedFileName { get; set; }
        
        public string ConvertedFilePath { get; set; }
        
        public long? ConvertedFileSize { get; set; }
        
        [Required]
        public ConversionType ConversionType { get; set; }
        
        [Required]
        public ConversionStatus Status { get; set; } = ConversionStatus.Pending;
        
        public string ErrorMessage { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? CompletedAt { get; set; }
        
        public DateTime? ProcessingStartedAt { get; set; }
        
        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }

    public enum ConversionType
    {
        MDBToCSV, // Standardized on uppercase version
        MdvToPdf,
        PdfToDocx,
        JpgToPng,
        PngToJpg,
        JpgToWebp,
        PngToWebp,
        WebpToJpg,
        WebpToPng
    }

    public enum ConversionStatus
    {
        Pending,
        Processing,
        Completed,
        Failed
    }
}
