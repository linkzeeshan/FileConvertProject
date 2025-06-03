using System;
using System.Collections.Generic;

namespace FileConvertPro.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalConversions { get; set; }
        public int PendingConversions { get; set; }
        public int FailedConversions { get; set; }
        public IEnumerable<FileConversion> RecentConversions { get; set; } = new List<FileConversion>();
        public IEnumerable<ApplicationUser> RecentUsers { get; set; } = new List<ApplicationUser>();
        
        // Conversion type statistics
        public int ImageConversions { get; set; }
        public int DocumentConversions { get; set; }
        public int DatabaseConversions { get; set; }
        public int ActiveSubscribers { get; set; }
    }
}
