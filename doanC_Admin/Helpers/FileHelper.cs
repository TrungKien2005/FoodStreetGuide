namespace doanC_Admin.Helpers
{
    public static class FileHelper
    {
        /// <summary>
        /// Tạo tên file hợp lệ cho Android Resource
        /// </summary>
        /// <param name="originalFileName">Tên file gốc</param>
        /// <param name="prefix">Tiền tố (loc, qr, aud)</param>
        /// <returns>Tên file hợp lệ</returns>
        public static string GenerateValidFileName(string originalFileName, string prefix = "img")
        {
            var extension = Path.GetExtension(originalFileName).ToLower();

            // Chỉ cho phép các định dạng ảnh
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            if (!allowedExtensions.Contains(extension))
            {
                extension = ".png"; // Mặc định là png
            }

            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 12);

            // Tên file: prefix_thoigian_uniqueid.extension
            return $"{prefix}_{timestamp}_{uniqueId}{extension}";
        }

        /// <summary>
        /// Kiểm tra tên file có hợp lệ không
        /// </summary>
        public static bool IsValidFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return false;

            // Kiểm tra ký tự đầu tiên
            char firstChar = fileName[0];
            if (!char.IsLetter(firstChar)) return false;

            // Kiểm tra các ký tự còn lại
            foreach (char c in fileName)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '.')
                {
                    return false;
                }
            }

            return true;
        }
    }
}