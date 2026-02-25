using capstone_backend.Business.DTOs.Moderation;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Cors.Infrastructure;
using OpenAI.Moderations;
using System.Text.RegularExpressions;

namespace capstone_backend.Business.Services
{
    public class ModerationService : IModerationService
    {
        private readonly ModerationClient? _client;

        private readonly HashSet<string> _bannedWords;
        private readonly List<string> _bannedPhrases;

        private const double HARD_BLOCK = 0.75;
        private const double PENDING = 0.25;

        public ModerationService(IWebHostEnvironment env, ModerationClient? client = null)
        {
            var filePath = Path.Combine(env.ContentRootPath, "Resources", "BadWords", "banned-words.txt");

            _bannedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _bannedPhrases = new List<string>();

            if (File.Exists(filePath))
            {
                var lines = File.ReadAllLines(filePath)
                    .Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#"))
                    .Select(x => x.Trim().ToLower())
                    .Distinct();

                foreach (var line in lines)
                {
                    if (line.Contains(" "))
                    {
                        _bannedPhrases.Add(line);
                    }
                    else
                    {
                        _bannedWords.Add(line);
                    }
                }
            }

            _client = client;
        }

        public (bool IsValid, string? Message) CheckContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return (true, null);
            var normalized = content.ToLower();

            var badPhrase = _bannedPhrases.FirstOrDefault(p => normalized.Contains(p));
            if (badPhrase != null) return (false, $"Nội dung chứa cụm từ cấm: '{badPhrase}'");

            var words = Regex.Split(normalized, @"\P{L}+");

            foreach (var word in words)
            {
                if (_bannedWords.Contains(word))
                {
                    return (false, $"Nội dung chứa từ cấm: '{word}'");
                }
            }

            return (true, null);
        }

        public async Task<ModerationResult> TestAsync(string content)
        {
            var response = await _client.ClassifyTextAsync(content);
            var moderation = response;

            return moderation;
        }

        public async Task<List<ModerationResultDto>> CheckContentByAIService(List<string> inputs)
        {
            var finalResults = new List<ModerationResultDto>();
            if (_client == null || inputs == null || !inputs.Any())
            {
                return finalResults;
            }

            try
            {
                var response = await _client.ClassifyTextAsync(inputs);
                var aiResults = response.Value;
                int imageCounter = 0;

                for (int i = 0; i < aiResults.Count; i++)
                {
                    string currentInput = inputs[i];
                    string label;

                    if (IsImageUrl(currentInput))
                    {
                        imageCounter++;
                        label = $"Hình ảnh {imageCounter}";
                    }
                    else
                    {
                        label = "Nội dung chữ";
                    }

                    var dto = EvaluateResult(aiResults[i], label);
                    finalResults.Add(dto);
                }

                return finalResults;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AI Error]: {ex.Message}");
                finalResults.Add(ModerationResultDto.Safe("Hệ thống"));
                return finalResults;
            }
        }

        private ModerationResultDto EvaluateResult(ModerationResult result, string label)
        {
            if (!result.Flagged)
                return ModerationResultDto.Safe(label);

            var scores = new List<(string Name, float score)>
            {
                ("Khiêu dâm", result.Sexual.Score),
                ("Khiêu dâm/Trẻ vị thành niên", result.SexualMinors.Score),
                ("Quấy rối", result.Harassment.Score),
                ("Quấy rối/Đe doạ", result.HarassmentThreatening.Score),
                ("Thù ghét", result.Hate.Score),
                ("Thù ghét/Đe doạ", result.HarassmentThreatening.Score),
                ("Bất hợp pháp", result.Illicit.Score),
                ("Bất hợp pháp/Bạo lực", result.IllicitViolent.Score),
                ("Tự hại", result.SelfHarm.Score),
                ("Tự hại/Cố ý", result.SelfHarmIntent.Score),
                ("Hướng dẫn tự làm hại bản thân", result.SelfHarmInstructions.Score),
                ("Bạo lực", result.Violence.Score),
                ("Bạo lực/Mô tả chi tiết", result.ViolenceGraphic.Score)
            };

            var top = scores.OrderByDescending(s => s.score).First();
            if (top.score >= HARD_BLOCK)
                return ModerationResultDto.Block(label, $"Nội dung bị chặn do vi phạm: {top.Name} (score: {Math.Round(top.score, 2)})");
            else if (top.score >= PENDING)
                return ModerationResultDto.NeedReview(label, $"Nội dung cần được xem xét do có dấu hiệu vi phạm: {top.Name} (score: {Math.Round(top.score, 2)})");
            else
                return ModerationResultDto.Safe(label);
        }

        private bool IsImageUrl(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;

            bool isUri = Uri.TryCreate(input, UriKind.Absolute, out Uri? uriResult)
                         && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            return isUri;
        }
    }
}
