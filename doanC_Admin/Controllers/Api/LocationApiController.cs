using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using doanC_Admin.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace doanC_Admin.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationApiController : ControllerBase
    {
        private readonly FoodStreetGuideDBContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public LocationApiController(FoodStreetGuideDBContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
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

                // ✅ Nếu có lastSync, chỉ lấy dữ liệu thay đổi sau thời gian đó
                if (lastSync.HasValue && lastSync.Value > DateTime.MinValue)
                {
                    query = query.Where(l => l.UpdatedAt > lastSync.Value || l.CreatedAt > lastSync.Value);

                    // Log để debug
                    Console.WriteLine($"[API] Incremental sync from {lastSync.Value:yyyy-MM-dd HH:mm:ss}");
                }

                var locations = await query
                    .OrderByDescending(l => l.UpdatedAt)
                    .ToListAsync();

                // ✅ Thêm header để client biết thông tin sync
                Response.Headers.Append("X-Last-Sync", DateTime.UtcNow.ToString("o"));
                Response.Headers.Append("X-Total-Count", locations.Count.ToString());

                if (lastSync.HasValue)
                {
                    Response.Headers.Append("X-Incremental", "true");
                }

                Console.WriteLine($"[API] Returning {locations.Count} locations");
                return Ok(locations);
            }
            catch (Exception ex)
            {
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
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/LocationApi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<LocationPoint>> GetLocationById(int id)
        {
            var location = await _context.LocationPoints.FindAsync(id);
            if (location == null)
                return NotFound(new { error = "Không tìm thấy địa điểm" });
            return Ok(location);
        }

        // GET: api/LocationApi/category/{category}
        [HttpGet("category/{category}")]
        public async Task<ActionResult<IEnumerable<LocationPoint>>> GetLocationsByCategory(string category)
        {
            var locations = await _context.LocationPoints
                .Where(l => l.Category != null && l.Category.Contains(category))
                .ToListAsync();
            return Ok(locations);
        }

        // GET: api/LocationApi/search/{keyword}
        [HttpGet("search/{keyword}")]
        public async Task<ActionResult<IEnumerable<LocationPoint>>> SearchLocations(string keyword)
        {
            var locations = await _context.LocationPoints
                .Where(l => l.Name.Contains(keyword) ||
                            (l.Description != null && l.Description.Contains(keyword)) ||
                            (l.Address != null && l.Address.Contains(keyword)))
                .ToListAsync();
            return Ok(locations);
        }

        // GET: api/LocationApi/image/{fileName}
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
                return BadRequest(new { error = ex.Message });
            }
        }
        // GET: api/LocationApi/paged?page=1&pageSize=20
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
                return StatusCode(500, new { error = ex.Message });
            }
        }
        // GET: api/LocationApi/changes?since=2024-01-01T00:00:00
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

        var deletedIds = await GetDeletedIdsSince(since); // Cần bảng tracking

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
        return StatusCode(500, new { error = ex.Message });
    }
}

// Cần thêm bảng DeletedLogs để track xóa
private async Task<List<int>> GetDeletedIdsSince(DateTime since)
{
    try
    {
        // Nếu có bảng DeletedLogs
        // return await _context.DeletedLogs
        //     .Where(d => d.DeletedAt > since)
        //     .Select(d => d.PointId)
        //     .ToListAsync();
        
        return new List<int>(); // Tạm thời trả về rỗng
    }
    catch
    {
        return new List<int>();
    }
}

        // POST: api/LocationApi/upload-image
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
                return BadRequest(new { error = ex.Message });
            }
        }

        // POST: api/LocationApi
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

                return Ok(location);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // POST: api/LocationApi/batch
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

                return Ok(new
                {
                    count = locations.Count,
                    message = $"Đã thêm thành công {locations.Count} địa điểm"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // PUT: api/LocationApi/5
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
                return Ok(existingLocation);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // DELETE: api/LocationApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLocation(int id)
        {
            try
            {
                var location = await _context.LocationPoints.FindAsync(id);
                if (location == null)
                    return NotFound(new { error = "Không tìm thấy địa điểm" });

                // ✅ THÊM: GHI LOG XÓA VÀO BẢNG DELETEDLOGS
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
                    await _context.SaveChangesAsync(); // Lưu log trước khi xóa
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"[DeleteLocation] Log error: {logEx.Message}");
                    // Vẫn tiếp tục xóa dù log lỗi
                }

                // Xóa file ảnh nếu có
                if (!string.IsNullOrEmpty(location.Image))
                {
                    DeleteImageFile(location.Image);
                }

                _context.LocationPoints.Remove(location);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Đã xóa thành công", deletedId = id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpPost("ApproveLocation")]
        public async Task<IActionResult> ApproveLocation([FromBody] ApproveLocationRequest request)
        {
            try
            {
                var location = await _context.LocationPoints.FindAsync(request.PointId);
                if (location == null)
                    return BadRequest(new { success = false, message = "Không tìm thấy địa điểm" });

                location.IsApproved = request.IsApproved;
                location.ApprovedBy = GetCurrentAdminId();
                location.ApprovedAt = DateTime.Now;
                location.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public class ApproveLocationRequest
        {
            public int PointId { get; set; }
            public bool IsApproved { get; set; }
        }

        private int GetCurrentAdminId()
        {
            // Lấy từ session hoặc context
            var adminId = HttpContext.Session.GetString("AdminId");
            return int.TryParse(adminId, out int id) ? id : 1;
        }

        // ========== PHƯƠNG THỨC HỖ TRỢ ==========

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
                Console.WriteLine($"Lỗi khi xóa ảnh: {ex.Message}");
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
}