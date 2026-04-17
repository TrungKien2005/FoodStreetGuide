using System;

namespace doanC_Admin.Models
{
    public class ActiveUserDto
    {
        public int AdminId { get; set; }
        public string Username { get; set; } = string.Empty;
        public DateTime LoginTime { get; set; }
        public DateTime LastActivity { get; set; }
        public string IPAddress { get; set; } = string.Empty;
    }
}