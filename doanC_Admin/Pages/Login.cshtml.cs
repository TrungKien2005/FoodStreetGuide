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

        public void OnGet()
        {
            // Nếu đã đăng nhập, chuyển hướng theo role
            if (HttpContext.Session.GetString("AdminId") != null)
            {
                var role = HttpContext.Session.GetString("Role");
                if (role == "Manager")
                    Response.Redirect("/Owner/Dashboard");
                else
                    Response.Redirect("/Dashboard");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Vui lòng nhập tên đăng nhập và mật khẩu";
                return Page();
            }

            var user = await _context.AdminUsers
                .FirstOrDefaultAsync(u => u.Username == Username && u.PasswordHash == Password);

            if (user == null)
            {
                ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng";
                return Page();
            }

            if (user.IsActive == false)
            {
                ErrorMessage = "Tài khoản đã bị khóa";
                return Page();
            }

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
            await _context.SaveChangesAsync();

            // Cập nhật LastLogin
            user.LastLogin = DateTime.Now;
            await _context.SaveChangesAsync();

            // Tạo session
            HttpContext.Session.SetString("AdminId", user.AdminId.ToString());
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("FullName", user.FullName ?? user.Username);
            HttpContext.Session.SetString("Role", user.Role ?? "Admin");

            // Ghi vào bảng session đang hoạt động
            var activeSession = new AdminSession
            {
                AdminId = user.AdminId,
                Username = user.Username,
                LoginTime = DateTime.Now,
                LastActivity = DateTime.Now,
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                IsActive = true
            };
            _context.AdminSessions.Add(activeSession);
            await _context.SaveChangesAsync();

            // Chuyển hướng theo role
            if (user.Role == "Manager")
            {
                return RedirectToPage("/Owner/Dashboard");
            }

            return RedirectToPage("/Dashboard");
        }
    }
}