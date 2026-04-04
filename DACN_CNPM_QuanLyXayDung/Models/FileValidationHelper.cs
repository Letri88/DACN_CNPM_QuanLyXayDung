using System.IO;
using Microsoft.AspNetCore.Http;

namespace DACN_CNPM_QuanLyXayDung.Models
{
    public static class FileValidationHelper
    {
        public static bool IsPdfFile(IFormFile file)
        {
            if (file == null || file.Length < 5) return false;

            using (var stream = file.OpenReadStream())
            {
                var buffer = new byte[5];
                stream.Read(buffer, 0, 5);
                // Magic numbers for PDF are %PDF- (0x25 0x50 0x44 0x46 0x2D)
                if (buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46 && buffer[4] == 0x2D)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
