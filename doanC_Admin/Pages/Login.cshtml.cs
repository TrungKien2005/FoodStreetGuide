using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using doanC_Admin.Models;
using System;
using System.Threading.Tasks;

namespace doanC_Admin.Pages
{
    public class LoginModel : PageModel
    {
        private readonly FoodStreetGuideDBContext _context;

        public LoginModel(FoodStreetGuideDBContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    ErrorMessage = "Vui lòng nhập tên đăng nhập và mật khẩu";
                    return Page();
                }

                var user = await _context.AdminUsers
                    .FirstOrDefaultAsync(u => u.Username == Username && u.IsActive == true);

                if (user == null)
                {
                    ErrorMessage = "Tên đăng nhập không tồn tại hoặc bị khóa";
                    return Page();
                }

                if (user.PasswordHash != Password)
                {
                    ErrorMessage = "Mật khẩu không chính xác";
                    return Page();
                }

                // Cập nhật thời gian đăng nhập cuối
                user.LastLogin = DateTime.Now;
                await _context.SaveChangesAsync();

                // Lưu Session
                HttpContext.Session.SetString("AdminId", user.AdminId.ToString());
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("FullName", user.FullName ?? user.Username);
                HttpContext.Session.SetString("Role", user.Role ?? "Viewer");

                // Ghi log đăng nhập
                var loginLog = new AdminLoginLog
                {
                    AdminId = user.AdminId,
                    Username = user.Username,
                    LoginTime = DateTime.Now,
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    DeviceInfo = Request.Headers["User-Agent"].ToString(),
                    Status = "Success"
                };
                _context.AdminLoginLogs.Add(loginLog);

                // Tạo session active
                var session = new AdminSession
                {
                    AdminId = user.AdminId,
                    Username = user.Username,
                    LoginTime = DateTime.Now,
                    LastActivity = DateTime.Now,
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    IsActive = true
                };
                _context.AdminSessions.Add(session);
                await _context.SaveChangesAsync();

                // ✅ Redirect theo Role (CHỈ 1 LẦN)
                var role = user.Role?.ToLower() ?? "viewer";

                if (role == "owner" || role == "manager")
                {
                    return RedirectToPage("/Owner/Dashboard");
                }
                else
                {
                    return RedirectToPage("/Dashboard");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi hệ thống: {ex.Message}";
                return Page();
            }
        }

        public IActionResult OnGet()
        {
            var role = HttpContext.Session.GetString("Role");
            if (!string.IsNullOrEmpty(role))
            {
                if (role == "Owner" || role == "Manager")
                    return RedirectToPage("/Owner/Dashboard");
                return RedirectToPage("/Dashboard");
            }
            return Page();
        }
    }
}