using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using doanC_Admin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace doanC_Admin.Pages
{
    [IgnoreAntiforgeryToken]  // 👈 Đặt ở đây để tránh lỗi 400
    public class AccountManagementModel : PageModel
    {
        private readonly FoodStreetGuideDBContext _context;

        // ========== PHÂN TRANG ==========
        public List<AdminUser> Users { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;

        public AccountManagementModel(FoodStreetGuideDBContext context)
        {
            _context = context;
        }

        // ========== GET: Hiển thị danh sách tài khoản ==========
        public async Task<IActionResult> OnGetAsync(int currentPage = 1)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "SuperAdmin" && role != "Admin")
            {
                return RedirectToPage("/Dashboard");
            }

            CurrentPage = currentPage;
            var totalRecords = await _context.AdminUsers.CountAsync();
            TotalPages = (int)Math.Ceiling(totalRecords / (double)PageSize);

            Users = await _context.AdminUsers
                .OrderByDescending(u => u.AdminId)
                .Skip((currentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            return Page();
        }

        // ========== POST: Tạo tài khoản mới (Nhận JSON) ==========
        public async Task<IActionResult> OnPostCreate([FromBody] CreateUserRequest request)
        {
            try
            {
                Console.WriteLine($"📥 Received request: {System.Text.Json.JsonSerializer.Serialize(request)}");

                if (request == null)
                {
                    return new JsonResult(new { success = false, message = "Invalid request body" });
                }

                // Kiểm tra dữ liệu
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.PasswordHash))
                {
                    return new JsonResult(new { success = false, message = "Vui lòng nhập đầy đủ thông tin!" });
                }

                // Kiểm tra username đã tồn tại
                if (await _context.AdminUsers.AnyAsync(u => u.Username == request.Username))
                {
                    return new JsonResult(new { success = false, message = "Tên đăng nhập đã tồn tại!" });
                }

                // Tạo user mới
                var newUser = new AdminUser
                {
                    Username = request.Username,
                    PasswordHash = request.PasswordHash,
                    FullName = request.FullName,
                    Email = request.Email,
                    Role = string.IsNullOrEmpty(request.Role) ? "Viewer" : request.Role,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.AdminUsers.Add(newUser);
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ User created with ID: {newUser.AdminId}");

                // Nếu role là Manager, tạo thêm StoreOwner
                if (newUser.Role == "Manager")
                {
                    var storeOwner = new StoreOwner
                    {
                        AdminId = newUser.AdminId,
                        Status = "Approved",
                        ApprovedAt = DateTime.Now
                    };
                    _context.StoreOwners.Add(storeOwner);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"✅ StoreOwner created for Manager ID: {newUser.AdminId}");
                }

                return new JsonResult(new { success = true, message = "Tạo tài khoản thành công!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return new JsonResult(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // ========== GET: Lấy thông tin user theo ID ==========
        public async Task<IActionResult> OnGetUser(int id)
        {
            var user = await _context.AdminUsers.FindAsync(id);
            if (user == null)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy tài khoản" });
            }

            return new JsonResult(new
            {
                success = true,
                adminId = user.AdminId,
                username = user.Username,
                fullName = user.FullName,
                email = user.Email,
                role = user.Role,
                isActive = user.IsActive
            });
        }

        // ========== POST: Reset mật khẩu ==========
        public async Task<IActionResult> OnPostResetPassword(int id)
        {
            try
            {
                var user = await _context.AdminUsers.FindAsync(id);
                if (user == null)
                {
                    return new JsonResult(new { success = false, message = "Không tìm thấy tài khoản" });
                }

                user.PasswordHash = "123456";
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, message = "Đã reset mật khẩu thành 123456!" });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        // ========== GET: Export Excel ==========
        public async Task<IActionResult> OnGetExportExcel()
        {
            var users = await _context.AdminUsers
                .OrderByDescending(u => u.AdminId)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("ID,Username,FullName,Email,Role,IsActive,LastLogin,CreatedAt");

            foreach (var user in users)
            {
                sb.AppendLine($"{user.AdminId},{EscapeCsv(user.Username)},{EscapeCsv(user.FullName)},{EscapeCsv(user.Email)},{user.Role},{(user.IsActive == true ? "Active" : "Inactive")},{user.LastLogin},{user.CreatedAt}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"users_{DateTime.Now:yyyyMMdd}.csv");
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") || value.Contains("\""))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }

        // ========== POST: Cập nhật tài khoản ==========
        public async Task<IActionResult> OnPostUpdate()
        {
            try
            {
                var adminId = int.Parse(Request.Form["AdminId"]);
                var password = Request.Form["PasswordHash"].ToString();
                var fullName = Request.Form["FullName"].ToString();
                var email = Request.Form["Email"].ToString();
                var role = Request.Form["Role"].ToString();

                var user = await _context.AdminUsers.FindAsync(adminId);
                if (user == null)
                {
                    return new JsonResult(new { success = false, message = "Không tìm thấy tài khoản" });
                }

                if (!string.IsNullOrEmpty(password))
                {
                    user.PasswordHash = password;
                }
                user.FullName = fullName;
                user.Email = email;
                user.Role = role;
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                if (role == "Manager")
                {
                    var existingOwner = await _context.StoreOwners.FirstOrDefaultAsync(s => s.AdminId == user.AdminId);
                    if (existingOwner == null)
                    {
                        var storeOwner = new StoreOwner
                        {
                            AdminId = user.AdminId,
                            Status = "Approved",
                            ApprovedAt = DateTime.Now
                        };
                        _context.StoreOwners.Add(storeOwner);
                        await _context.SaveChangesAsync();
                    }
                }

                return new JsonResult(new { success = true, message = "Cập nhật thành công!" });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // ========== POST: Khóa/Mở khóa tài khoản ==========
        public async Task<IActionResult> OnPostToggleStatus(int id, bool isActive)
        {
            try
            {
                var user = await _context.AdminUsers.FindAsync(id);
                if (user == null)
                {
                    return new JsonResult(new { success = false, message = "Không tìm thấy tài khoản" });
                }

                if (user.Role == "SuperAdmin" && isActive == false)
                {
                    return new JsonResult(new { success = false, message = "Không thể khóa tài khoản SuperAdmin!" });
                }

                user.IsActive = isActive;
                user.UpdatedAt = DateTime.Now;

                if (!isActive)
                {
                    var sessions = await _context.AdminSessions
                        .Where(s => s.AdminId == id && s.IsActive == true)
                        .ToListAsync();
                    foreach (var session in sessions)
                    {
                        session.IsActive = false;
                    }
                }

                await _context.SaveChangesAsync();

                var statusText = isActive ? "mở khóa" : "khóa";
                return new JsonResult(new { success = true, message = $"Đã {statusText} tài khoản thành công!" });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // ========== POST: Xóa tài khoản ==========
        public async Task<IActionResult> OnPostDelete(int id)
        {
            try
            {
                var user = await _context.AdminUsers.FindAsync(id);
                if (user == null)
                {
                    return new JsonResult(new { success = false, message = "Không tìm thấy tài khoản" });
                }

                if (user.Role == "SuperAdmin")
                {
                    return new JsonResult(new { success = false, message = "Không thể xóa tài khoản SuperAdmin!" });
                }

                var storeOwner = await _context.StoreOwners.FirstOrDefaultAsync(s => s.AdminId == id);
                if (storeOwner != null)
                {
                    _context.StoreOwners.Remove(storeOwner);
                }

                var sessions = await _context.AdminSessions.Where(s => s.AdminId == id).ToListAsync();
                _context.AdminSessions.RemoveRange(sessions);

                var loginLogs = await _context.AdminLoginLogs.Where(l => l.AdminId == id).ToListAsync();
                _context.AdminLoginLogs.RemoveRange(loginLogs);

                _context.AdminUsers.Remove(user);
                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, message = "Xóa tài khoản thành công!" });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }
    }

    // ========== REQUEST MODEL ==========
    public class CreateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}