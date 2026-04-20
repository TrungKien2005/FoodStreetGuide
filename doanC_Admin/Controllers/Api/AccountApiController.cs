using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using doanC_Admin.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace doanC_Admin.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AccountApiController : ControllerBase
    {
        private readonly FoodStreetGuideDBContext _context;

        public AccountApiController(FoodStreetGuideDBContext context)
        {
            _context = context;
        }

        [HttpPost("CreateUser")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                if (await _context.AdminUsers.AnyAsync(u => u.Username == request.Username))
                    return BadRequest(new { success = false, message = "Tên đăng nhập đã tồn tại" });

                var user = new AdminUser
                {
                    Username = request.Username,
                    PasswordHash = request.Password,
                    FullName = request.FullName,
                    Email = request.Email,
                    Role = request.Role,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.AdminUsers.Add(user);
                await _context.SaveChangesAsync();

                // Tạo StoreOwner nếu role là Manager
                if (request.Role == "Manager")
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

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("GetUser/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.AdminUsers.FindAsync(id);
            if (user == null)
                return NotFound();

            return Ok(new
            {
                user.AdminId,
                user.Username,
                user.FullName,
                user.Email,
                user.Role,
                user.IsActive
            });
        }

        [HttpPost("UpdateUser")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request)
        {
            try
            {
                var user = await _context.AdminUsers.FindAsync(request.AdminId);
                if (user == null)
                    return BadRequest(new { success = false, message = "Không tìm thấy tài khoản" });

                if (!string.IsNullOrEmpty(request.Password))
                    user.PasswordHash = request.Password;

                user.FullName = request.FullName;
                user.Email = request.Email;
                user.Role = request.Role;
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("ToggleStatus")]
        public async Task<IActionResult> ToggleStatus(int id, bool isActive)
        {
            var user = await _context.AdminUsers.FindAsync(id);
            if (user == null)
                return BadRequest(new { success = false, message = "Không tìm thấy tài khoản" });

            user.IsActive = isActive;
            user.UpdatedAt = DateTime.Now;

            // Nếu khóa tài khoản, đóng tất cả session
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
            return Ok(new { success = true });
        }

        [HttpDelete("DeleteUser")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.AdminUsers.FindAsync(id);
            if (user == null)
                return BadRequest(new { success = false, message = "Không tìm thấy tài khoản" });

            if (user.Role == "SuperAdmin")
                return BadRequest(new { success = false, message = "Không thể xóa tài khoản SuperAdmin" });

            _context.AdminUsers.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }

    public class CreateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string Role { get; set; } = "Viewer";
    }

    public class UpdateUserRequest
    {
        public int AdminId { get; set; }
        public string? Password { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string Role { get; set; } = "Viewer";
    }
}