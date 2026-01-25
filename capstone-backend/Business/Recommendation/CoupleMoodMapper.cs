namespace capstone_backend.Business.Recommendation;

/// <summary>
/// Maps individual moods (from AWS Rekognition) to couple mood types
/// Implements business rules for combining 2 individual moods into 12 couple moods
/// 
/// Individual Moods: HAPPY, DISGUSTED, SURPRISED, CALM, FEAR, CONFUSED, ANGRY, SAD
/// Couple Mood Types (12): 
/// 1. Shared Happiness (Vui chung)
/// 2. Mutual Calm (Yên tĩnh)
/// 3. Comfort-Seeking (Cần an ủi)
/// 4. Stress and Tension (Căng thẳng)
/// 5. Emotional Imbalance (Lệch pha)
/// 6. Exploration Mood (Hứng thú khám phá)
/// 7. Playful but Sensitive (Vui nhưng dễ tổn thương)
/// 8. Reassurance Needed (Cần trấn an)
/// 9. Low-Intimacy Boundary (Giảm thân mật)
/// 10. Resolution Mode (Cần hòa giải)
/// 11. High-Energy Divergence (Năng lượng không đồng đều)
/// 12. Neutral / Mixed Mood (Trung tính)
/// </summary>
public static class CoupleMoodMapper
{
    /// <summary>
    /// Maps two individual moods to a couple mood type
    /// </summary>
    public static string MapToCoupleeMood(string mood1, string mood2)
    {
        var normalizedMood1 = NormalizeMood(mood1);
        var normalizedMood2 = NormalizeMood(mood2);

        // Check Rule 1: Shared Happiness – Vui chung
        // Khi ít nhất 1 người HAPPY và người kia không tiêu cực mạnh (CALM, SURPRISED, CONFUSED)
        if (IsHappiness(normalizedMood1, normalizedMood2))
            return "Shared Happiness";

        // Check Rule 2: Mutual Calm – Yên tĩnh
        // Khi cả hai CALM hoặc một CALM + người kia trung tính (CONFUSED)
        if (IsMutualCalm(normalizedMood1, normalizedMood2))
            return "Mutual Calm";

        // Check Rule 3: Comfort-Seeking – Cần an ủi
        // Khi 1 người SAD và người kia không ANGRY/DISGUSTED
        if (IsComfortSeeking(normalizedMood1, normalizedMood2))
            return "Comfort-Seeking";

        // Check Rule 4: Stress & Tension – Căng thẳng
        // Khi một hoặc cả hai: ANGRY, FEAR, DISGUSTED
        if (IsStressTension(normalizedMood1, normalizedMood2))
            return "Stress & Tension";

        // Check Rule 5: Emotional Imbalance – Lệch pha cảm xúc
        // Khi 1 người HAPPY vs người kia SAD / ANGRY / FEAR / DISGUSTED
        if (IsEmotionalImbalance(normalizedMood1, normalizedMood2))
            return "Emotional Imbalance";

        // Check Rule 6: Exploration Mood – Hứng thú khám phá
        // Khi bất kỳ người nào SURPRISED và người còn lại HAPPY/CALM/CONFUSED
        if (IsExplorationMood(normalizedMood1, normalizedMood2))
            return "Exploration Mood";

        // Check Rule 7: Playful but Sensitive – Vui nhưng dễ tổn thương
        // HAPPY + SAD, HAPPY + CONFUSED, HAPPY + FEAR
        if (IsPlayfulSensitive(normalizedMood1, normalizedMood2))
            return "Playful but Sensitive";

        // Check Rule 8: Reassurance Needed – Cần được trấn an
        // Khi 1 người FEAR/CONFUSED và người kia CALM/SURPRISED
        if (IsReassuranceNeeded(normalizedMood1, normalizedMood2))
            return "Reassurance Needed";

        // Check Rule 9: Low-Intimacy Boundary – Giảm thân mật
        // Khi bất kỳ người nào DISGUSTED
        if (IsLowIntimacyBoundary(normalizedMood1, normalizedMood2))
            return "Low-Intimacy Boundary";

        // Check Rule 10: Resolution Mode – Cần hòa giải
        // ANGRY + SAD, ANGRY + CONFUSED, SAD + DISGUSTED
        if (IsResolutionMode(normalizedMood1, normalizedMood2))
            return "Resolution Mode";

        // Check Rule 11: High-Energy Divergence – Năng lượng không đồng đều
        // HAPPY + ANGRY, SURPRISED + ANGRY
        if (IsHighEnergyDivergence(normalizedMood1, normalizedMood2))
            return "High-Energy Divergence";

        // Check Rule 12: Neutral / Mixed Mood – Trung tính
        // CONFUSED + CALM, CONFUSED + SURPRISED (or fallback)
        return "Neutral / Mixed Mood";
    }

    private static string NormalizeMood(string mood)
    {
        return mood?.ToUpperInvariant() ?? "";
    }

    /// <summary>
    /// Rule 1: Shared Happiness – Vui chung
    /// Khi ít nhất 1 người HAPPY và người kia không tiêu cực mạnh (CALM, SURPRISED, CONFUSED)
    /// </summary>
    private static bool IsHappiness(string mood1, string mood2)
    {
        var hasHappy = mood1 == "HAPPY" || mood2 == "HAPPY";
        var otherIsPositive = (mood1 == "HAPPY" && IsPositiveOrNeutral(mood2)) ||
                              (mood2 == "HAPPY" && IsPositiveOrNeutral(mood1));
        return hasHappy && otherIsPositive;
    }

    private static bool IsPositiveOrNeutral(string mood)
    {
        return mood == "CALM" || mood == "SURPRISED" || mood == "CONFUSED" || mood == "HAPPY";
    }

    /// <summary>
    /// Rule 2: Mutual Calm – Yên tĩnh
    /// Khi cả hai CALM hoặc một CALM + người kia trung tính (CONFUSED)
    /// </summary>
    private static bool IsMutualCalm(string mood1, string mood2)
    {
        if (mood1 == "CALM" && mood2 == "CALM")
            return true;
        if ((mood1 == "CALM" && mood2 == "CONFUSED") || (mood1 == "CONFUSED" && mood2 == "CALM"))
            return true;
        return false;
    }

    /// <summary>
    /// Rule 3: Comfort-Seeking – Cần an ủi
    /// Khi 1 người SAD và người kia không ANGRY/DISGUSTED
    /// </summary>
    private static bool IsComfortSeeking(string mood1, string mood2)
    {
        var hasSad = mood1 == "SAD" || mood2 == "SAD";
        if (!hasSad)
            return false;

        var otherMood = mood1 == "SAD" ? mood2 : mood1;
        return otherMood != "ANGRY" && otherMood != "DISGUSTED";
    }

    /// <summary>
    /// Rule 4: Stress and Tension – Căng thẳng
    /// Khi một hoặc cả hai: ANGRY, FEAR, DISGUSTED
    /// </summary>
    private static bool IsStressTension(string mood1, string mood2)
    {
        var negativeEmotions = new[] { "ANGRY", "FEAR", "DISGUSTED" };
        return negativeEmotions.Contains(mood1) || negativeEmotions.Contains(mood2);
    }

    /// <summary>
    /// Rule 5: Emotional Imbalance – Lệch pha cảm xúc
    /// Khi 1 người HAPPY vs người kia SAD / ANGRY / FEAR / DISGUSTED
    /// </summary>
    private static bool IsEmotionalImbalance(string mood1, string mood2)
    {
        var negativeEmotions = new[] { "SAD", "ANGRY", "FEAR", "DISGUSTED" };
        var hasHappy = mood1 == "HAPPY" || mood2 == "HAPPY";
        var hasNegative = negativeEmotions.Contains(mood1) || negativeEmotions.Contains(mood2);
        return hasHappy && hasNegative;
    }

    /// <summary>
    /// Rule 6: Exploration Mood – Hứng thú khám phá
    /// Khi bất kỳ người nào SURPRISED và người còn lại HAPPY/CALM/CONFUSED
    /// </summary>
    private static bool IsExplorationMood(string mood1, string mood2)
    {
        var hasSurprised = mood1 == "SURPRISED" || mood2 == "SURPRISED";
        if (!hasSurprised)
            return false;

        var otherMood = mood1 == "SURPRISED" ? mood2 : mood1;
        return otherMood == "HAPPY" || otherMood == "CALM" || otherMood == "CONFUSED";
    }

    /// <summary>
    /// Rule 7: Playful but Sensitive – Vui nhưng dễ tổn thương
    /// HAPPY + SAD, HAPPY + CONFUSED, HAPPY + FEAR
    /// </summary>
    private static bool IsPlayfulSensitive(string mood1, string mood2)
    {
        var sensitivePairs = new[]
        {
            ("HAPPY", "SAD"),
            ("SAD", "HAPPY"),
            ("HAPPY", "CONFUSED"),
            ("CONFUSED", "HAPPY"),
            ("HAPPY", "FEAR"),
            ("FEAR", "HAPPY")
        };

        return sensitivePairs.Contains((mood1, mood2));
    }

    /// <summary>
    /// Rule 8: Reassurance Needed – Cần được trấn an
    /// Khi 1 người FEAR/CONFUSED và người kia CALM/SURPRISED
    /// </summary>
    private static bool IsReassuranceNeeded(string mood1, string mood2)
    {
        var needsReassurance = new[] { "FEAR", "CONFUSED" };
        var canReassure = new[] { "CALM", "SURPRISED" };

        return (needsReassurance.Contains(mood1) && canReassure.Contains(mood2)) ||
               (needsReassurance.Contains(mood2) && canReassure.Contains(mood1));
    }

    /// <summary>
    /// Rule 9: Low-Intimacy Boundary – Giảm thân mật
    /// Khi bất kỳ người nào DISGUSTED
    /// </summary>
    private static bool IsLowIntimacyBoundary(string mood1, string mood2)
    {
        return mood1 == "DISGUSTED" || mood2 == "DISGUSTED";
    }

    /// <summary>
    /// Rule 10: Resolution Mode – Cần hòa giải
    /// ANGRY + SAD, ANGRY + CONFUSED, SAD + DISGUSTED
    /// </summary>
    private static bool IsResolutionMode(string mood1, string mood2)
    {
        var resolutionPairs = new[]
        {
            ("ANGRY", "SAD"),
            ("SAD", "ANGRY"),
            ("ANGRY", "CONFUSED"),
            ("CONFUSED", "ANGRY"),
            ("SAD", "DISGUSTED"),
            ("DISGUSTED", "SAD")
        };

        return resolutionPairs.Contains((mood1, mood2));
    }

    /// <summary>
    /// Rule 11: High-Energy Divergence – Năng lượng không đồng đều
    /// HAPPY + ANGRY, SURPRISED + ANGRY
    /// </summary>
    private static bool IsHighEnergyDivergence(string mood1, string mood2)
    {
        var divergencePairs = new[]
        {
            ("HAPPY", "ANGRY"),
            ("ANGRY", "HAPPY"),
            ("SURPRISED", "ANGRY"),
            ("ANGRY", "SURPRISED")
        };

        return divergencePairs.Contains((mood1, mood2));
    }
}
