using capstone_backend.Business.Interfaces;
using System.Text.RegularExpressions;

namespace capstone_backend.Business.Services
{
    public class ModerationService : IModerationService
    {
        private readonly HashSet<string> _bannedWords;
        private readonly List<string> _bannedPhrases;

        public ModerationService(IWebHostEnvironment env)
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
    }
}
