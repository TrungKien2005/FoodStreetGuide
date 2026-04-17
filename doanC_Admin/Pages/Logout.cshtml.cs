using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using doanC_Admin.Hubs;
using doanC_Admin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace doanC_Admin.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly FoodStreetGuideDBContext _context;
        private readonly IHubContext<DashboardHub> _hubContext;

        public LogoutModel(FoodStreetGuideDBContext context, IHubContext<DashboardHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Lấy thông tin session hiện tại
            var adminId = HttpContext.Session.GetString("AdminId");
            var sessionToken = HttpContext.Session.GetString("SessionToken");
            var username = HttpContext.Session.GetString("Username");

            Debug.WriteLine($"[Logout] === BẮT ĐẦU ĐĂNG XUẤT ===");
            Debug.WriteLine($"[Logout] AdminId: {adminId}");
            Debug.WriteLine($"[Logout] Username: {username}");

            if (!string.IsNullOrEmpty(adminId))
            {
                var adminIdInt = int.Parse(adminId);

                // ✅ 1. CẬP NHẬT SESSION THÀNH INACTIVE
                var sessions = await _context.AdminSessions
                    .Where(s => s.AdminId == adminIdInt && s.IsActive == true)
                    .ToListAsync();

                foreach (var session in sessions)
                {
                    session.IsActive = false;
                    session.LastActivity = DateTime.Now;
                    Debug.WriteLine($"[Logout] Đã vô hiệu hóa session ID: {session.SessionId}");
                }

                // ✅ 2. CẬP NHẬT LOGOUT TIME TRONG ADMINLOGINLOGS
                var latestLog = await _context.AdminLoginLogs
                    .Where(l => l.AdminId == adminIdInt && l.LogoutTime == null)
                    .OrderByDescending(l => l.LoginTime)
                    .FirstOrDefaultAsync();

                if (latestLog != null)
                {
                    latestLog.LogoutTime = DateTime.Now;
                    Debug.WriteLine($"[Logout] Đã cập nhật LogoutTime cho LogId: {latestLog.LogId}");
                }

                await _context.SaveChangesAsync();

                // ✅ 3. GỬI REAL-TIME UPDATE ĐẾN DASHBOARD
                try
                {
                    await _hubContext.Clients.All.SendAsync("RefreshDashboard");
                    await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                        "🚪 Đăng xuất", $"{username ?? "Người dùng"} đã đăng xuất", "info");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Logout] Lỗi gửi SignalR: {ex.Message}");
                }
            }

            // ✅ 4. XÓA SESSION HOÀN TOÀN
            HttpContext.Session.Clear();
            HttpContext.Response.Cookies.Delete(".AspNetCore.Session");
            HttpContext.Response.Cookies.Delete("FoodStreetGuide.Auth");

            Debug.WriteLine("[Logout] Đã xóa session hoàn toàn");
            Debug.WriteLine($"[Logout] === KẾT THÚC ĐĂNG XUẤT ===");

            // ✅ 5. CHUYỂN HƯỚNG VỀ TRANG INDEX (LOADING)
            return RedirectToPage("/Index");
        }
    }
}