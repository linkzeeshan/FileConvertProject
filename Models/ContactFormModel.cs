using System.ComponentModel.DataAnnotations;

namespace FileConvertPro.Models
{
    public class ContactFormModel
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        public string Name { get; set; }
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; }
        
        [Phone(ErrorMessage = "Please enter a valid phone number")]
        public string Phone { get; set; }
        
        [Required(ErrorMessage = "Subject is required")]
        [StringLength(200, ErrorMessage = "Subject cannot be longer than 200 characters")]
        public string Subject { get; set; }
        
        [Required(ErrorMessage = "Message is required")]
        [StringLength(2000, ErrorMessage = "Message cannot be longer than 2000 characters")]
        public string Message { get; set; }
        
        [Required(ErrorMessage = "You must agree to our privacy policy")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to our privacy policy")]
        public bool AgreeToPrivacyPolicy { get; set; }
    }
}
