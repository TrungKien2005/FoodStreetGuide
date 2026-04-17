using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using doanC_Admin.Models;

namespace doanC_Admin.Pages.LocationPoints
{
    public class IndexModel : PageModel
    {
        private readonly FoodStreetGuideDBContext _context;
        private readonly int _pageSize = 10;  // Số dòng mỗi trang

        public IndexModel(FoodStreetGuideDBContext context)
        {
            _context = context;
        }

        public List<LocationPoint> LocationPoints { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public async Task OnGetAsync(int currentPage = 1)
        {
            CurrentPage = currentPage;
            var totalRecords = await _context.LocationPoints.CountAsync();
            TotalPages = (int)Math.Ceiling(totalRecords / (double)_pageSize);

            // 👉 SỬA: Sắp xếp theo PointId tăng dần (1 -> N)
            LocationPoints = await _context.LocationPoints
                .OrderBy(l => l.PointId)  // Từ 1 đến N
                .Skip((currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToListAsync();
        }
    }
}