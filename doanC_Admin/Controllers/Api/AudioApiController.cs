using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using doanC_Admin.Models;
using doanC_Admin.Helpers;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace doanC_Admin.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AudioApiController : ControllerBase
    {
        private readonly FoodStreetGuideDBContext _context;

        public AudioApiController(FoodStreetGuideDBContext context)
        {
            _context = context;
        }

        [HttpPost("UploadAudio")]
        public async Task<IActionResult> UploadAudio([FromForm] int pointId, [FromForm] int languageId, IFormFile audioFile)
        {
            try
            {
                var location = await _context.LocationPoints.FindAsync(pointId);
                if (location == null)
                    return BadRequest(new { success = false, message = "Không tìm thấy địa điểm" });

                if (audioFile == null || audioFile.Length == 0)
                    return BadRequest(new { success = false, message = "Vui lòng chọn file audio" });

                // Kiểm tra định dạng file
                var allowedExtensions = new[] { ".mp3", ".wav", ".ogg", ".m4a" };
                var extension = Path.GetExtension(audioFile.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                    return BadRequest(new { success = false, message = "Định dạng file không hỗ trợ" });

                // Tạo tên file hợp lệ
                var fileName = FileHelper.GenerateValidFileName(audioFile.FileName, "aud");
                var audioPath = GetAudioPath();
                var filePath = Path.Combine(audioPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await audioFile.CopyToAsync(stream);
                }

                var audioRecord = new AudioFile
                {
                    PointId = pointId,
                    LanguageId = languageId,
                    FileName = fileName,
                    FilePath = $"/audio/{fileName}",
                    Duration = 0,
                    CreatedAt = DateTime.Now
                };

                _context.AudioFiles.Add(audioRecord);
                await _context.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("DeleteAudio")]
        public async Task<IActionResult> DeleteAudio(int id)
        {
            var audio = await _context.AudioFiles.FindAsync(id);
            if (audio == null)
                return BadRequest(new { success = false, message = "Không tìm thấy audio" });

            // Xóa file vật lý
            var audioPath = GetAudioPath();
            var filePath = Path.Combine(audioPath, audio.FileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            _context.AudioFiles.Remove(audio);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        private string GetAudioPath()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var solutionDirectory = Directory.GetParent(currentDirectory)?.FullName ?? currentDirectory;
            var audioPath = Path.Combine(solutionDirectory, "FoodStreetGuide", "Resources", "Audio");

            if (!Directory.Exists(audioPath))
            {
                Directory.CreateDirectory(audioPath);
            }
            return audioPath;
        }
    }
}