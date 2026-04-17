using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using doanC_Admin.Models;
using QRCoder;
using System;
using System.IO;
using System.Threading.Tasks;
using QRCodeLib = QRCoder.QRCode;  // 👈 Alias để tránh ambiguous

namespace doanC_Admin.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class QrApiController : ControllerBase
    {
        private readonly FoodStreetGuideDBContext _context;

        public QrApiController(FoodStreetGuideDBContext context)
        {
            _context = context;
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
                var qrName = string.IsNullOrEmpty(request.Name) ? $"QR_{request.PointId}" : request.Name;

                using var generator = new QRCodeGenerator();
                using var qrCodeData = generator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrCodeBytes = qrCode.GetGraphic(request.Size);

                // Lưu ảnh vào thư mục
                var fileName = $"qr_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}.png";
                var qrPath = GetQrImagesPath();
                var filePath = Path.Combine(qrPath, fileName);
                await System.IO.File.WriteAllBytesAsync(filePath, qrCodeBytes);

                // Lưu vào database - dùng Models.QRCode để tránh ambiguous
                var qrRecord = new Models.QRCode
                {
                    PointId = request.PointId,
                    Name = qrName,
                    QrContent = qrContent,
                    QrImagePath = fileName,
                    CreatedAt = DateTime.Now
                };

                _context.QRCodes.Add(qrRecord);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
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
            var qr = await _context.QRCodes.FindAsync(id);
            if (qr == null)
                return NotFound();

            // Nếu có ảnh lưu trữ, trả về file
            if (!string.IsNullOrEmpty(qr.QrImagePath))
            {
                var qrPath = GetQrImagesPath();
                var filePath = Path.Combine(qrPath, qr.QrImagePath);

                if (System.IO.File.Exists(filePath))
                {
                    var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                    return File(fileBytes, "image/png", $"{qr.Name ?? "qrcode"}.png");
                }
            }

            // Nếu không có ảnh, tạo mới từ content
            var qrContent = qr.QrContent ?? $"https://foodstreetguide.com/location/{qr.PointId}";
            using var generator = new QRCodeGenerator();
            using var qrCodeData = generator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeBytes = qrCode.GetGraphic(300);

            return File(qrCodeBytes, "image/png", $"{qr.Name ?? "qrcode"}.png");
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
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("DeleteQR")]
        public async Task<IActionResult> DeleteQR(int id)
        {
            var qr = await _context.QRCodes.FindAsync(id);
            if (qr == null)
                return NotFound();

            // Xóa file ảnh
            if (!string.IsNullOrEmpty(qr.QrImagePath))
            {
                DeleteOldImage(qr.QrImagePath);
            }

            _context.QRCodes.Remove(qr);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        private string GetQrImagesPath()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var solutionDirectory = Directory.GetParent(currentDirectory)?.FullName ?? currentDirectory;
            var qrPath = Path.Combine(solutionDirectory, "FoodStreetGuide", "Resources", "qr");

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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting image: {ex.Message}");
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