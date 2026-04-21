using System.IO;
using doanC_Admin.Hubs;
using doanC_Admin.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace doanC_Admin.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class LocationApiController : ControllerBase
    {
        private readonly FoodStreetGuideDBContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly ILogger<LocationApiController> _logger;

        public LocationApiController(
            FoodStreetGuideDBContext context,
            IWebHostEnvironment webHostEnvironment,
            IHubContext<DashboardHub> hubContext,
            ILogger<LocationApiController> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _hubContext = hubContext;
            _logger = logger;
        }

        // ========== CORE API - HỖ TRỢ INCREMENTAL SYNC ==========

        /// <summary>
        /// GET: api/LocationApi
        /// Lấy danh sách địa điểm (hỗ trợ sync incremental)
        /// </summary>
        /// <param name="lastSync">Thời gian sync lần cuối (ISO format)</param>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LocationPoint>>> GetAllLocations([FromQuery] DateTime? lastSync = null)
        {
            try
            {
                IQueryable<LocationPoint> query = _context.LocationPoints;

                if (lastSync.HasValue && lastSync.Value > DateTime.MinValue)
                {
                    query = query.Where(l => l.UpdatedAt > lastSync.Value || l.CreatedAt > lastSync.Value);
                    _logger.LogInformation($"[API] Incremental sync from {lastSync.Value:yyyy-MM-dd HH:mm:ss}");
                }

                var locations = await query
                    .OrderByDescending(l => l.UpdatedAt)
                    .ToListAsync();

                Response.Headers.Append("X-Last-Sync", DateTime.UtcNow.ToString("o"));
                Response.Headers.Append("X-Total-Count", locations.Count.ToString());

                if (lastSync.HasValue)
                {
                    Response.Headers.Append("X-Incremental", "true");
                }

                _logger.LogInformation($"[API] Returning {locations.Count} locations");
                return Ok(locations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllLocations");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/LocationApi/version
        /// Lấy thông tin phiên bản dữ liệu (để kiểm tra có dữ liệu mới không)
        /// </summary>
        [HttpGet("version")]
        public async Task<ActionResult<object>> GetVersion()
        {
            try
            {
                var lastUpdate = await _context.LocationPoints
                    .MaxAsync(l => l.UpdatedAt);

                var totalCount = await _context.LocationPoints.CountAsync();

                return Ok(new
                {
                    LastUpdate = lastUpdate,
                    TotalCount = totalCount,
                    ServerTime = DateTime.Now,
                    ApiVersion = "1.0.0"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetVersion");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/LocationApi/5
        /// Lấy địa điểm theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<LocationPoint>> GetLocationById(int id)
        {
            try
            {
                var location = await _context.LocationPoints.FindAsync(id);
                if (location == null)
                    return NotFound(new { error = "Không tìm thấy địa điểm" });
                return Ok(location);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetLocationById for id {id}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/LocationApi/category/{category}
        /// Lấy địa điểm theo danh mục
        /// </summary>
        [HttpGet("category/{category}")]
        public async Task<ActionResult<IEnumerable<LocationPoint>>> GetLocationsByCategory(string category)
        {
            try
            {
                var locations = await _context.LocationPoints
                    .Where(l => l.Category != null && l.Category.Contains(category))
                    .ToListAsync();
                return Ok(locations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetLocationsByCategory for category {category}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/LocationApi/search/{keyword}
        /// Tìm kiếm địa điểm theo từ khóa
        /// </summary>
        [HttpGet("search/{keyword}")]
        public async Task<ActionResult<IEnumerable<LocationPoint>>> SearchLocations(string keyword)
        {
            try
            {
                var locations = await _context.LocationPoints
                    .Where(l => l.Name.Contains(keyword) ||
                                (l.Description != null && l.Description.Contains(keyword)) ||
                                (l.Address != null && l.Address.Contains(keyword)))
                    .ToListAsync();
                return Ok(locations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in SearchLocations for keyword {keyword}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/LocationApi/image/{fileName}
        /// Lấy ảnh địa điểm
        /// </summary>
        [HttpGet("image/{fileName}")]
        public IActionResult GetImage(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                    return BadRequest(new { error = "Tên file không hợp lệ" });

                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", fileName);

                if (!System.IO.File.Exists(imagePath))
                {
                    var defaultImagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "default.png");
                    if (System.IO.File.Exists(defaultImagePath))
                    {
                        var defaultImageBytes = System.IO.File.ReadAllBytes(defaultImagePath);
                        return File(defaultImageBytes, "image/png");
                    }
                    return NotFound(new { error = "Không tìm thấy ảnh" });
                }

                var imageBytes = System.IO.File.ReadAllBytes(imagePath);
                var contentType = GetContentType(fileName);
                return File(imageBytes, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetImage for fileName {fileName}");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/LocationApi/paged?page=1&pageSize=20
        /// Lấy danh sách địa điểm có phân trang
        /// </summary>
        [HttpGet("paged")]
        public async Task<ActionResult<object>> GetPagedLocations(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] DateTime? lastSync = null)
        {
            try
            {
                IQueryable<LocationPoint> query = _context.LocationPoints;

                if (lastSync.HasValue && lastSync.Value > DateTime.MinValue)
                {
                    query = query.Where(l => l.UpdatedAt > lastSync.Value || l.CreatedAt > lastSync.Value);
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var locations = await query
                    .OrderByDescending(l => l.UpdatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new
                {
                    Data = locations,
                    Pagination = new
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = totalPages,
                        HasNextPage = page < totalPages,
                        HasPrevPage = page > 1
                    },
                    Sync = new
                    {
                        LastSync = DateTime.UtcNow,
                        IsIncremental = lastSync.HasValue
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPagedLocations");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/LocationApi/changes?since=2024-01-01T00:00:00
        /// Lấy các thay đổi từ thời điểm chỉ định
        /// </summary>
        [HttpGet("changes")]
        public async Task<ActionResult<object>> GetChangesSince([FromQuery] DateTime since)
        {
            try
            {
                if (since == DateTime.MinValue)
                {
                    return BadRequest(new { error = "Missing 'since' parameter" });
                }

                var newLocations = await _context.LocationPoints
                    .Where(l => l.CreatedAt > since)
                    .ToListAsync();

                var updatedLocations = await _context.LocationPoints
                    .Where(l => l.UpdatedAt > since && l.CreatedAt <= since)
                    .ToListAsync();

                var deletedIds = await GetDeletedIdsSince(since);

                return Ok(new
                {
                    New = newLocations,
                    Updated = updatedLocations,
                    DeletedIds = deletedIds,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetChangesSince for since {since}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// POST: api/LocationApi/upload-image
        /// Upload ảnh địa điểm
        /// </summary>
        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { error = "Vui lòng chọn file ảnh" });

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest(new { error = "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif, webp)" });

                var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                var uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");

                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                var filePath = Path.Combine(uploadFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                return Ok(new { fileName = uniqueFileName, url = $"/images/{uniqueFileName}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UploadImage");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// POST: api/LocationApi
        /// Thêm địa điểm mới
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<LocationPoint>> AddLocation([FromForm] LocationPoint location, IFormFile? imageFile)
        {
            try
            {
                if (location == null)
                    return BadRequest(new { error = "Dữ liệu không hợp lệ" });

                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileName = await SaveImageFile(imageFile);
                    location.Image = fileName;
                }

                location.CreatedAt = DateTime.Now;
                location.UpdatedAt = DateTime.Now;

                _context.LocationPoints.Add(location);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Added new location: {location.Name} (ID: {location.PointId})");
                return Ok(location);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddLocation");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// POST: api/LocationApi/batch
        /// Thêm nhiều địa điểm cùng lúc
        /// </summary>
        [HttpPost("batch")]
        public async Task<IActionResult> BatchInsertLocations([FromBody] List<LocationPoint> locations)
        {
            try
            {
                if (locations == null || locations.Count == 0)
                    return BadRequest(new { error = "Không có dữ liệu để thêm" });

                foreach (var location in locations)
                {
                    location.CreatedAt = DateTime.Now;
                    location.UpdatedAt = DateTime.Now;
                    _context.LocationPoints.Add(location);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Batch inserted {locations.Count} locations");
                return Ok(new
                {
                    count = locations.Count,
                    message = $"Đã thêm thành công {locations.Count} địa điểm"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BatchInsertLocations");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// PUT: api/LocationApi/5
        /// Cập nhật địa điểm
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLocation(int id, [FromForm] LocationPoint location, IFormFile? imageFile)
        {
            try
            {
                if (id != location.PointId)
                    return BadRequest(new { error = "ID không khớp" });

                var existingLocation = await _context.LocationPoints.FindAsync(id);
                if (existingLocation == null)
                    return NotFound(new { error = "Không tìm thấy địa điểm" });

                if (imageFile != null && imageFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(existingLocation.Image))
                    {
                        DeleteImageFile(existingLocation.Image);
                    }
                    var fileName = await SaveImageFile(imageFile);
                    existingLocation.Image = fileName;
                }

                existingLocation.Name = location.Name;
                existingLocation.Description = location.Description;
                existingLocation.Latitude = location.Latitude;
                existingLocation.Longitude = location.Longitude;
                existingLocation.Address = location.Address;
                existingLocation.Category = location.Category;
                existingLocation.Rating = location.Rating;
                existingLocation.ReviewCount = location.ReviewCount;
                existingLocation.OpeningHours = location.OpeningHours;
                existingLocation.PriceRange = location.PriceRange;
                existingLocation.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Updated location: {existingLocation.Name} (ID: {existingLocation.PointId})");
                return Ok(existingLocation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in UpdateLocation for id {id}");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// DELETE: api/LocationApi/5
        /// Xóa địa điểm
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLocation(int id)
        {
            try
            {
                var location = await _context.LocationPoints.FindAsync(id);
                if (location == null)
                    return NotFound(new { error = "Không tìm thấy địa điểm" });

                // Ghi log xóa vào bảng DeletedLogs
                try
                {
                    var deletedLog = new DeletedLog
                    {
                        PointId = location.PointId,
                        Name = location.Name ?? "Unknown",
                        DeletedAt = DateTime.Now,
                        DeletedBy = User?.Identity?.Name ?? "System"
                    };
                    _context.DeletedLogs.Add(deletedLog);
                    await _context.SaveChangesAsync();
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, $"Failed to log deletion for location {id}");
                }

                // Xóa file ảnh nếu có
                if (!string.IsNullOrEmpty(location.Image))
                {
                    DeleteImageFile(location.Image);
                }

                _context.LocationPoints.Remove(location);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Deleted location: {location.Name} (ID: {location.PointId})");
                return Ok(new { message = "Đã xóa thành công", deletedId = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in DeleteLocation for id {id}");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// POST: api/LocationApi/ApproveLocation
        /// Duyệt hoặc từ chối địa điểm
        /// </summary>
        [HttpPost("ApproveLocation")]
        public async Task<IActionResult> ApproveLocation([FromBody] ApproveLocationRequest request)
        {
            try
            {
                // Kiểm tra request
                if (request == null || request.PointId <= 0)
                {
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                // Tìm địa điểm cần duyệt
                var poi = await _context.LocationPoints
                    .Include(l => l.Owner)
                    .ThenInclude(o => o.AdminUser)
                    .FirstOrDefaultAsync(l => l.PointId == request.PointId);

                if (poi == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy địa điểm" });
                }

                // Lấy thông tin admin đang duyệt
                var adminId = HttpContext.Session.GetString("AdminId");
                var adminName = HttpContext.Session.GetString("Username") ?? "Admin";
                var adminRole = HttpContext.Session.GetString("Role") ?? "Admin";

                // Kiểm tra quyền duyệt (chỉ Admin hoặc SuperAdmin mới được duyệt)
                if (adminRole != "Admin" && adminRole != "SuperAdmin")
                {
                    return BadRequest(new { success = false, message = "Bạn không có quyền duyệt địa điểm" });
                }

                // Xử lý duyệt hoặc từ chối
                if (request.IsApproved)
                {
                    poi.IsApproved = true;
                    poi.ApprovedAt = DateTime.Now;
                    poi.ApprovedBy = string.IsNullOrEmpty(adminId) ? null : int.Parse(adminId);
                    poi.RejectionReason = null;
                    poi.UpdatedAt = DateTime.Now;
                }
                else
                {
                    poi.IsApproved = false;
                    poi.RejectionReason = string.IsNullOrEmpty(request.RejectionReason)
                        ? "Địa điểm không đáp ứng tiêu chuẩn"
                        : request.RejectionReason;
                    poi.ApprovedAt = null;
                    poi.ApprovedBy = null;
                    poi.UpdatedAt = DateTime.Now;
                }

                // Cập nhật số lượng pending/approved/rejected của Owner
                if (poi.OwnerId.HasValue)
                {
                    var owner = await _context.StoreOwners
                        .Include(o => o.AdminUser)
                        .FirstOrDefaultAsync(o => o.OwnerId == poi.OwnerId);

                    if (owner != null)
                    {
                        if (request.IsApproved)
                        {
                            owner.ApprovedLocations = (owner.ApprovedLocations.GetValueOrDefault(0)) + 1;
                            if ((owner.PendingLocations ?? 0) > 0)
                            {
                                owner.PendingLocations = (owner.PendingLocations ?? 0) - 1;
                            }
                        }
                        else
                        {
                            owner.RejectedLocations = (owner.RejectedLocations ?? 0) + 1;
                            if ((owner.PendingLocations ?? 0) > 0)
                            {
                                owner.PendingLocations = (owner.PendingLocations ?? 0) - 1;
                            }
                        }
                        owner.UpdatedAt = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();

                // Gửi thông báo real-time qua SignalR
                var status = request.IsApproved ? "đã được duyệt" : "bị từ chối";
                var reason = request.IsApproved ? "" : $"\nLý do: {poi.RejectionReason}";

                await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                    request.IsApproved ? "✅ Địa điểm được duyệt" : "❌ Địa điểm bị từ chối",
                    $"Địa điểm '{poi.Name}' của bạn {status}{reason}",
                    request.IsApproved ? "success" : "error");

                if (poi.Owner?.AdminUser != null)
                {
                    await _hubContext.Clients.All.SendAsync("NotifyOwner",
                        poi.Owner.AdminUser.Username,
                        request.IsApproved ? "Địa điểm được duyệt" : "Địa điểm bị từ chối",
                        $"Địa điểm '{poi.Name}' của bạn {status}");
                }

                await _hubContext.Clients.All.SendAsync("RefreshDashboard");
                await _hubContext.Clients.All.SendAsync("RefreshPendingList");
                await _hubContext.Clients.All.SendAsync("UpdateStats");

                _logger.LogInformation($"Admin {adminName} (ID: {adminId}) đã {(request.IsApproved ? "duyệt" : "từ chối")} địa điểm '{poi.Name}' (ID: {poi.PointId})");

                return Ok(new
                {
                    success = true,
                    message = request.IsApproved ? "Đã duyệt địa điểm thành công" : "Đã từ chối địa điểm",
                    data = new
                    {
                        PointId = poi.PointId,
                        Name = poi.Name,
                        IsApproved = poi.IsApproved,
                        ApprovedAt = poi.ApprovedAt,
                        ApprovedBy = adminName,
                        RejectionReason = poi.RejectionReason
                    }
                });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error when approving location");
                return BadRequest(new { success = false, message = "Lỗi database: " + (dbEx.InnerException?.Message ?? dbEx.Message) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when approving location");
                return BadRequest(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // ========== PHƯƠNG THỨC HỖ TRỢ ==========

        private async Task<List<int>> GetDeletedIdsSince(DateTime since)
        {
            try
            {
                return await _context.DeletedLogs
                    .Where(d => d.DeletedAt > since)
                    .Select(d => d.PointId)
                    .ToListAsync();
            }
            catch
            {
                return new List<int>();
            }
        }

        private async Task<string> SaveImageFile(IFormFile imageFile)
        {
            var uniqueFileName = $"{Guid.NewGuid()}_{imageFile.FileName}";
            var uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");

            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            var filePath = Path.Combine(uploadFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return uniqueFileName;
        }

        private void DeleteImageFile(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName)) return;

                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", fileName);
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error deleting image file: {fileName}");
            }
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }
    }

    // ========== REQUEST/ RESPONSE CLASSES ==========

    public class ApproveLocationRequest
    {
        public int PointId { get; set; }
        public bool IsApproved { get; set; }
        public string? RejectionReason { get; set; }
    }
}