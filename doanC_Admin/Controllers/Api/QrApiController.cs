using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using doanC_Admin.Models;
using QRCoder;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace doanC_Admin.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class QrApiController : ControllerBase
    {
        private readonly FoodStreetGuideDBContext _context;
        private readonly IWebHostEnvironment _environment;

        public QrApiController(FoodStreetGuideDBContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpPost("GenerateQR")]
        public async Task<IActionResult> GenerateQR([FromBody] GenerateQRRequest request)
        {
            try
            {
                var location = await _context.LocationPoints
                    .FirstOrDefaultAsync(l => l.PointId == request.PointId);

                if (location == null)
                    return BadRequest(new { success = false, message = "Không tìm thấy địa điểm" });

                var qrContent = $"https://foodstreetguide.com/location/{request.PointId}";
                var qrName = string.IsNullOrEmpty(request.Name) ? $"QR_{request.PointId}_{DateTime.Now:yyyyMMddHHmmss}" : request.Name;

                // Tạo QR Code
                using var generator = new QRCodeGenerator();
                using var qrCodeData = generator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrCodeBytes = qrCode.GetGraphic(request.Size);

                // Lưu ảnh vào wwwroot/qr
                var fileName = $"qr_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}.png";
                var qrPath = GetQrImagesPath();
                var filePath = Path.Combine(qrPath, fileName);
                await System.IO.File.WriteAllBytesAsync(filePath, qrCodeBytes);

                // Lưu vào database
                var qrRecord = new Models.QRCode
                {
                    PointId = request.PointId,
                    Name = qrName,
                    QrContent = qrContent,
                    QrImagePath = fileName, // Chỉ lưu tên file
                    CreatedAt = DateTime.Now
                };

                _context.QRCodes.Add(qrRecord);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Tạo QR Code thành công",
                    qrId = qrRecord.QrId,
                    qrImagePath = $"/qr/{fileName}",
                    qrName = qrName,
                    locationName = location.Name
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("GetQRImage")]
        public async Task<IActionResult> GetQRImage(int id)
        {
            try
            {
                var qr = await _context.QRCodes.FindAsync(id);
                if (qr == null)
                    return NotFound(new { success = false, message = "Không tìm thấy QR Code" });

                // Nếu có ảnh lưu trữ trong wwwroot/qr, trả về file
                if (!string.IsNullOrEmpty(qr.QrImagePath))
                {
                    var qrPath = GetQrImagesPath();
                    var filePath = Path.Combine(qrPath, qr.QrImagePath);

                    if (System.IO.File.Exists(filePath))
                    {
                        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                        return File(fileBytes, "image/png");
                    }
                }

                // Nếu không có ảnh hoặc ảnh bị mất, tạo mới từ content
                var qrContent = qr.QrContent ?? $"https://foodstreetguide.com/location/{qr.PointId}";
                using var generator = new QRCodeGenerator();
                using var qrCodeData = generator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrCodeBytes = qrCode.GetGraphic(300);

                // Lưu lại ảnh mới tạo
                var fileName = $"qr_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}.png";
                var qrPath2 = GetQrImagesPath();
                var filePath2 = Path.Combine(qrPath2, fileName);
                await System.IO.File.WriteAllBytesAsync(filePath2, qrCodeBytes);

                // Cập nhật database
                qr.QrImagePath = fileName;
                await _context.SaveChangesAsync();

                return File(qrCodeBytes, "image/png");
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("UpdateQR")]
        public async Task<IActionResult> UpdateQR([FromForm] int id, [FromForm] string name, IFormFile? imageFile)
        {
            try
            {
                var qr = await _context.QRCodes.FindAsync(id);
                if (qr == null)
                    return BadRequest(new { success = false, message = "Không tìm thấy QR Code" });

                // Cập nhật tên
                if (!string.IsNullOrEmpty(name))
                {
                    qr.Name = name;
                }

                // Cập nhật ảnh thủ công
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Kiểm tra định dạng file
                    var allowedExtensions = new[] { ".png", ".jpg", ".jpeg" };
                    var extension = Path.GetExtension(imageFile.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                    {
                        return BadRequest(new { success = false, message = "Chỉ chấp nhận file PNG, JPG, JPEG" });
                    }

                    // Kiểm tra kích thước (max 2MB)
                    if (imageFile.Length > 2 * 1024 * 1024)
                    {
                        return BadRequest(new { success = false, message = "Kích thước file không được vượt quá 2MB" });
                    }

                    // Xóa ảnh cũ
                    if (!string.IsNullOrEmpty(qr.QrImagePath))
                    {
                        DeleteOldImage(qr.QrImagePath);
                    }

                    // Lưu ảnh mới
                    var fileName = await SaveImageAsync(imageFile);
                    qr.QrImagePath = fileName;
                }

                await _context.SaveChangesAsync();

                // Trả về đường dẫn ảnh mới
                string imageUrl = $"/qr/{qr.QrImagePath}";
                return Ok(new { success = true, message = "Cập nhật thành công", imagePath = imageUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("DeleteQR")]
        public async Task<IActionResult> DeleteQR(int id)
        {
            try
            {
                var qr = await _context.QRCodes.FindAsync(id);
                if (qr == null)
                    return NotFound(new { success = false, message = "Không tìm thấy QR Code" });

                // Xóa file ảnh
                if (!string.IsNullOrEmpty(qr.QrImagePath))
                {
                    DeleteOldImage(qr.QrImagePath);
                }

                _context.QRCodes.Remove(qr);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Xóa QR Code thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("GetAllQRCodes")]
        public async Task<IActionResult> GetAllQRCodes()
        {
            try
            {
                var qrCodes = await _context.QRCodes
                    .OrderByDescending(q => q.CreatedAt)
                    .Select(q => new
                    {
                        q.QrId,
                        q.PointId,
                        q.Name,
                        q.QrContent,
                        QrImagePath = $"/qr/{q.QrImagePath}",
                        q.CreatedAt,
                        LocationName = _context.LocationPoints
                            .Where(l => l.PointId == q.PointId)
                            .Select(l => l.Name)
                            .FirstOrDefault() ?? "Không xác định"
                    })
                    .ToListAsync();

                return Ok(qrCodes);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("GetQRByPointId")]
        public async Task<IActionResult> GetQRByPointId(int pointId)
        {
            try
            {
                var qrCodes = await _context.QRCodes
                    .Where(q => q.PointId == pointId)
                    .OrderByDescending(q => q.CreatedAt)
                    .Select(q => new
                    {
                        q.QrId,
                        q.PointId,
                        q.Name,
                        q.QrContent,
                        QrImagePath = $"/qr/{q.QrImagePath}",
                        q.CreatedAt
                    })
                    .ToListAsync();

                return Ok(qrCodes);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        private string GetQrImagesPath()
        {
            // Sử dụng wwwroot/qr thay vì Resources/qr
            var qrPath = Path.Combine(_environment.WebRootPath, "qr");

            if (!Directory.Exists(qrPath))
            {
                Directory.CreateDirectory(qrPath);
            }
            return qrPath;
        }

        private async Task<string> SaveImageAsync(IFormFile imageFile)
        {
            var extension = Path.GetExtension(imageFile.FileName).ToLower();
            var fileName = $"qr_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}{extension}";
            var qrPath = GetQrImagesPath();
            var filePath = Path.Combine(qrPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return fileName;
        }

        private void DeleteOldImage(string imageName)
        {
            try
            {
                var qrPath = GetQrImagesPath();
                var filePath = Path.Combine(qrPath, imageName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    Console.WriteLine($"Deleted old image: {imageName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting image {imageName}: {ex.Message}");
            }
        }
    }

    public class GenerateQRRequest
    {
        public int PointId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Size { get; set; } = 200;
    }
}