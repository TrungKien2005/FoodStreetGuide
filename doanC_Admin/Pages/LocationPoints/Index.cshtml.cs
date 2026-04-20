using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using doanC_Admin.Models;
using doanC_Admin.Helpers;

namespace doanC_Admin.Pages.LocationPoints
{
    [Authorize("Admin", "SuperAdmin")]  // ✅ Chỉ Admin mới xem được
    public class IndexModel : PageModel
    {
        private readonly FoodStreetGuideDBContext _context;
        private readonly int _pageSize = 10;

        public IndexModel(FoodStreetGuideDBContext context)
        {
            _context = context;
        }

        public List<LocationPoint> LocationPoints { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalApprovedCount { get; set; }
        public int TotalPendingCount { get; set; }

        public async Task OnGetAsync(int currentPage = 1)
        {
            CurrentPage = currentPage;

            // ✅ CHỈ LẤY NHỮNG POI ĐÃ ĐƯỢC DUYỆT (IsApproved == true)
            var query = _context.LocationPoints.Where(l => l.IsApproved == true);

            // Thống kê số lượng
            TotalApprovedCount = await query.CountAsync();
            TotalPendingCount = await _context.LocationPoints.CountAsync(l => l.IsApproved == false);

            // Phân trang
            var totalRecords = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalRecords / (double)_pageSize);

            // Lấy dữ liệu theo trang
            LocationPoints = await query
                .OrderBy(l => l.PointId)
                .Skip((currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToListAsync();
        }
    }
}