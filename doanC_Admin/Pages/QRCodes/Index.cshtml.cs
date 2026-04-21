// Pages/QRCodes/Index.cshtml.cs
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using doanC_Admin.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace doanC_Admin.Pages.QRCodes
{
    public class IndexModel : PageModel
    {
        private readonly FoodStreetGuideDBContext _context;

        public IndexModel(FoodStreetGuideDBContext context)
        {
            _context = context;
        }

        public List<LocationPoint> Locations { get; set; } = new();
        public List<QRCodeDto> QRCodes { get; set; } = new();

        public async Task OnGetAsync()
        {
            Locations = await _context.LocationPoints
                .OrderBy(l => l.PointId)
                .ToListAsync();

            var qrRecords = await _context.QRCodes
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            foreach (var qr in qrRecords)
            {
                var location = await _context.LocationPoints
                    .FirstOrDefaultAsync(l => l.PointId == qr.PointId);

                // Tạo đường dẫn đúng cho ảnh từ wwwroot/qr
                string qrImagePath = null;
                if (!string.IsNullOrEmpty(qr.QrImagePath))
                {
                    // Đảm bảo đường dẫn có định dạng /qr/tên_file
                    qrImagePath = $"/qr/{qr.QrImagePath}";
                }

                QRCodes.Add(new QRCodeDto
                {
                    QrId = qr.QrId,
                    PointId = qr.PointId,
                    Name = qr.Name ?? $"QR_{qr.PointId}",
                    LocationName = location?.Name ?? "Không xác định",
                    QrContent = qr.QrContent ?? $"https://foodstreetguide.com/location/{qr.PointId}",
                    QrImagePath = qrImagePath,
                    CreatedAt = qr.CreatedAt ?? DateTime.Now,
                    IsActive = true
                });
            }
        }
    }

    public class QRCodeDto
    {
        public int QrId { get; set; }
        public int PointId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string QrContent { get; set; } = string.Empty;
        public string? QrImagePath { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}