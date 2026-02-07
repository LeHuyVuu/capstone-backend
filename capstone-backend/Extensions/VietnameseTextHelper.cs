using System.Globalization;
using System.Text;

namespace capstone_backend.Business.Helpers;

/// <summary>
/// Helper class for Vietnamese text processing
/// </summary>
public static class VietnameseTextHelper
{
    /// <summary>
    /// Remove Vietnamese accents from text
    /// "Phạm Nhật Vượng" -> "Pham Nhat Vuong"
    /// </summary>
    public static string RemoveVietnameseAccents(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        // Normalize to decomposed form (NFD)
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        // Replace Đ/đ manually (not handled by NFD)
        return stringBuilder.ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace("Đ", "D")
            .Replace("đ", "d");
    }

    /// <summary>
    /// Normalize text for search (lowercase + remove accents)
    /// "Phạm Nhật Vượng" -> "pham nhat vuong"
    /// </summary>
    public static string NormalizeForSearch(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        return RemoveVietnameseAccents(text).ToLower().Trim();
    }
}
