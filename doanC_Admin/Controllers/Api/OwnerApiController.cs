using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using doanC_Admin.Models;
using Microsoft.AspNetCore.SignalR;
using doanC_Admin.Hubs;
using Microsoft.AspNetCore.Authorization;

namespace doanC_Admin.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class OwnerApiController : ControllerBase
    {
        private readonly FoodStreetGuideDBContext _context;
        private readonly IHubContext<DashboardHub> _hubContext;

        public OwnerApiController(FoodStreetGuideDBContext context, IHubContext<DashboardHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // POST: api/OwnerApi/RegisterPoi
        [HttpPost("RegisterPoi")]
        public async Task<IActionResult> RegisterPoi([FromBody] PoiRegistrationDto registration)
        {
            try
            {
                var adminId = int.Parse(HttpContext.Session.GetString("AdminId") ?? "0");
                if (adminId == 0)
                    return Unauthorized(new { success = false, message = "Chưa đăng nhập" });

                // Lấy thông tin StoreOwner
                var owner = await _context.StoreOwners
                    .Include(o => o.AdminUser)
                    .FirstOrDefaultAsync(o => o.AdminId == adminId);

                if (owner == null)
                {
                    // Tạo StoreOwner mới nếu chưa có
                    owner = new StoreOwner
                    {
                        AdminId = adminId,
                        StoreName = registration.StoreName ?? registration.Name,
                        PhoneNumber = registration.Phone,
                        Email = registration.Email,
                        Status = "Pending",
                        CreatedAt = DateTime.Now
                    };
                    _context.StoreOwners.Add(owner);
                    await _context.SaveChangesAsync();
                }

                // Tạo POI mới
                var newPoi = new LocationPoint
                {
                    Name = registration.Name,
                    Description = registration.Description,
                    Latitude = registration.Latitude,
                    Longitude = registration.Longitude,
                    Address = registration.Address,
                    Category = registration.Category,
                    OpeningHours = registration.OpeningHours,
                    PriceRange = registration.PriceRange,
                    Image = registration.Image,
                    OwnerId = owner.OwnerId,
                    IsApproved = false,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.LocationPoints.Add(newPoi);

                // Cập nhật thống kê Owner
                owner.TotalLocations++;
                owner.PendingLocations++;

                await _context.SaveChangesAsync();

                // ✅ GỬI THÔNG BÁO REAL-TIME CHO ADMIN
                await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                    "📝 Đăng ký POI mới",
                    $"Chủ quán {owner.StoreName} vừa đăng ký POI: {registration.Name}",
                    "warning");

                await _hubContext.Clients.All.SendAsync("NewPendingPoi", registration.Name, owner.StoreName);
                await _hubContext.Clients.All.SendAsync("RefreshDashboard");
                await _hubContext.Clients.All.SendAsync("RefreshPendingList");

                return Ok(new
                {
                    success = true,
                    message = "Đã gửi yêu cầu đăng ký, admin sẽ duyệt trong thời gian sớm nhất",
                    poiId = newPoi.PointId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }

    public class PoiRegistrationDto
    {
        public string StoreName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Address { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string OpeningHours { get; set; } = string.Empty;
        public string PriceRange { get; set; } = string.Empty;
        public string? Image { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }
}