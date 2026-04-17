using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using doanC_Admin.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace doanC_Admin.Pages.LocationPoints
{
    public class DeleteModel : PageModel
    {
        private readonly FoodStreetGuideDBContext _context;

        public DeleteModel(FoodStreetGuideDBContext context)
        {
            _context = context;
        }

        [BindProperty]
        public LocationPoint LocationPoint { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            LocationPoint = await _context.LocationPoints.FindAsync(id);

            if (LocationPoint == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var location = await _context.LocationPoints.FindAsync(id);

            if (location == null)
            {
                return NotFound();
            }

            try
            {
                // Xóa ảnh liên quan
                if (!string.IsNullOrEmpty(location.Image))
                {
                    DeleteImageFile(location.Image);
                }

                // Xóa địa điểm
                _context.LocationPoints.Remove(location);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã xóa địa điểm '{location.Name}' thành công!";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi xóa: {ex.Message}";
                return RedirectToPage("./Index");
            }
        }

        private void DeleteImageFile(string imageName)
        {
            try
            {
                var currentDirectory = Directory.GetCurrentDirectory();
                var solutionPath = Path.GetFullPath(Path.Combine(currentDirectory, @"..\..\"));
                var mauiImagesPath = Path.Combine(solutionPath, "FoodStreetGuide", "Resources", "Images");
                var filePath = Path.Combine(mauiImagesPath, imageName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    Console.WriteLine($"Deleted image: {imageName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting image: {ex.Message}");
            }
        }
    }
}