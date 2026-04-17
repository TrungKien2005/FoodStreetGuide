using Microsoft.AspNetCore.Mvc.RazorPages;
using doanC_Admin.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace doanC_Admin.Pages.Shared.Reports
{
    public class IndexModel : PageModel
    {
        private readonly FoodStreetGuideDBContext _context;

        public IndexModel(FoodStreetGuideDBContext context)
        {
            _context = context;
        }

        public List<NotificationDto> Notifications { get; set; } = new();
        public List<TaskDto> Tasks { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Lấy thông báo từ database (nếu có bảng Notifications)
            // Hiện tại đang dữ liệu mẫu trong JavaScript

            // Có thể thêm các thống kê thực tế:
            // - Số địa điểm mới trong 7 ngày
            // - Số QR được tạo trong 24h
            // - Số audio mới được upload
        }
    }

    public class NotificationDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRead { get; set; }
    }

    public class TaskDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Deadline { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
    }
}