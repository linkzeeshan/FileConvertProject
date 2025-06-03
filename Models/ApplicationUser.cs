using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace FileConvertPro.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }
        public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Free;
        public DateTime? SubscriptionEndDate { get; set; }
        
        // Navigation properties
        public virtual ICollection<FileConversion> FileConversions { get; set; }
    }

    public enum SubscriptionTier
    {
        Free,
        Basic,
        Premium,
        Enterprise
    }
}
