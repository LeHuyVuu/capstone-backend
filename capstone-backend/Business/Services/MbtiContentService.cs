using capstone_backend.Business.DTOs.PersonalityTest;
using capstone_backend.Business.Interfaces;
using System.Text.Json;

namespace capstone_backend.Business.Services
{
    public class MbtiContentService : IMbtiContentService
    {
        private readonly Dictionary<string, MbtiDetail> _mbtiCache;

        public MbtiContentService(IWebHostEnvironment env)
        {
            var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
            var filePath = Path.Combine(webRoot, "assets", "mbti", "mbti.json");
            // --------------------

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[WARNING] File missing: {filePath}");
                _mbtiCache = new Dictionary<string, MbtiDetail>();
                return;
            }

            try
            {
                var jsonContent = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                _mbtiCache = JsonSerializer.Deserialize<Dictionary<string, MbtiDetail>>(jsonContent, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error when read file mbti.json: {ex.Message}");
                _mbtiCache = new Dictionary<string, MbtiDetail>();
            }
        }

        public MbtiDetail GetResult(string mbtiCode)
        {
            if (string.IsNullOrEmpty(mbtiCode)) return null;
            var key = mbtiCode.ToUpper().Trim();
            return (_mbtiCache != null && _mbtiCache.ContainsKey(key)) ? _mbtiCache[key] : null;
        }
    }
}
