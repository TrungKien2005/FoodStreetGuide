using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using doanC_Admin.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace doanC_Admin.Pages.Audios
{
    public class IndexModel : PageModel
    {
        private readonly FoodStreetGuideDBContext _context;

        public IndexModel(FoodStreetGuideDBContext context)
        {
            _context = context;
        }

        public List<AudioFile> Audios { get; set; } = new();
        public List<LocationPoint> Locations { get; set; } = new();

        public async Task OnGetAsync()
        {
            Audios = await _context.AudioFiles
                .Include(a => a.LocationPoint)
                .Include(a => a.Language)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            Locations = await _context.LocationPoints
                .OrderBy(l => l.Name)
                .ToListAsync();
        }
    }
}