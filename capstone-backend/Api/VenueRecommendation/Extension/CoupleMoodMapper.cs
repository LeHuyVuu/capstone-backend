namespace capstone_backend.Business.Recommendation;

/// <summary>
/// Maps individual moods (from AWS Rekognition) to couple mood types
/// Implements business rules for combining 2 individual moods into 12 couple moods
/// 
/// Individual Moods: HAPPY, DISGUSTED, SURPRISED, CALM, FEAR, CONFUSED, ANGRY, SAD
/// Couple Mood Types (12): 
/// 1. Shared Happiness (Vui vẻ)
/// 2. Mutual Calm (Yên tĩnh)
/// 3. Comfort-Seeking (Cần an ủi)
/// 4. Stress and Tension (Cân bằng)
/// 5. Emotional Imbalance (Hòa hợp)
/// 6. Exploration Mood (Khám phá)
/// 7. Playful but Sensitive (Tình cảm)
/// 8. Reassurance Needed (An tâm)
/// 9. Low-Intimacy Boundary (Thư giãn)
/// 10. Resolution Mode (Hòa giải)
/// 11. High-Energy Divergence (Động lực)
/// 12. Neutral / Mixed Mood (Trung lập)
/// </summary>
public static class CoupleMoodMapper
{
    /// <summary>
    /// Maps two individual moods to a couple mood type
    /// Thể hiện toàn bộ 36 cặp (64 tổ hợp) dựa trên ma trận CSV bằng if-else riêng biệt.
    /// </summary>
    public static string MapToCoupleeMood(string mood1, string mood2)
    {
        var m1 = (mood1 ?? "").ToUpper().Trim();
        var m2 = (mood2 ?? "").ToUpper().Trim();

        // --- 1. Resolution Mode (Hòa giải) ---
        if ((m1 == "ANGRY" && m2 == "SAD") || (m1 == "SAD" && m2 == "ANGRY"))
            return "Hòa giải";
        else if ((m1 == "ANGRY" && m2 == "CONFUSED") || (m1 == "CONFUSED" && m2 == "ANGRY"))
            return "Hòa giải";
        else if ((m1 == "SAD" && m2 == "DISGUSTED") || (m1 == "DISGUSTED" && m2 == "SAD"))
            return "Hòa giải";

        // --- 2. High-Energy Divergence (Động lực) ---
        else if ((m1 == "HAPPY" && m2 == "ANGRY") || (m1 == "ANGRY" && m2 == "HAPPY"))
            return "Động lực";
        else if ((m1 == "SURPRISED" && m2 == "ANGRY") || (m1 == "ANGRY" && m2 == "SURPRISED"))
            return "Động lực";

        // --- 3. Playful but Sensitive (Tình cảm) ---
        else if ((m1 == "HAPPY" && m2 == "SAD") || (m1 == "SAD" && m2 == "HAPPY"))
            return "Tình cảm";
        else if ((m1 == "HAPPY" && m2 == "FEAR") || (m1 == "FEAR" && m2 == "HAPPY"))
            return "Tình cảm";
        else if ((m1 == "HAPPY" && m2 == "CONFUSED") || (m1 == "CONFUSED" && m2 == "HAPPY"))
            return "Tình cảm";

        // --- 4. Emotional Imbalance (Hòa hợp) ---
        else if ((m1 == "HAPPY" && m2 == "DISGUSTED") || (m1 == "DISGUSTED" && m2 == "HAPPY"))
            return "Hòa hợp";

        // --- 5. Shared Happiness (Vui vẻ) ---
        else if (m1 == "HAPPY" && m2 == "HAPPY")
            return "Vui vẻ";
        else if ((m1 == "HAPPY" && m2 == "CALM") || (m1 == "CALM" && m2 == "HAPPY"))
            return "Vui vẻ";
        else if ((m1 == "HAPPY" && m2 == "SURPRISED") || (m1 == "SURPRISED" && m2 == "HAPPY"))
            return "Vui vẻ";

        // --- 6. Comfort-Seeking (Cần an ủi) ---
        else if (m1 == "SAD" && m2 == "SAD")
            return "Cần an ủi";
        else if ((m1 == "SAD" && m2 == "CALM") || (m1 == "CALM" && m2 == "SAD"))
            return "Cần an ủi";
        else if ((m1 == "SAD" && m2 == "SURPRISED") || (m1 == "SURPRISED" && m2 == "SAD"))
            return "Cần an ủi";
        else if ((m1 == "SAD" && m2 == "CONFUSED") || (m1 == "CONFUSED" && m2 == "SAD"))
            return "Cần an ủi";
        else if ((m1 == "SAD" && m2 == "FEAR") || (m1 == "FEAR" && m2 == "SAD"))
            return "Cần an ủi";

        // --- 7. Reassurance Needed (An tâm) ---
        else if ((m1 == "FEAR" && m2 == "CALM") || (m1 == "CALM" && m2 == "FEAR"))
            return "An tâm";
        else if ((m1 == "FEAR" && m2 == "SURPRISED") || (m1 == "SURPRISED" && m2 == "FEAR"))
            return "An tâm";

        // --- 8. Exploration Mood (Khám phá) ---
        else if (m1 == "SURPRISED" && m2 == "SURPRISED")
            return "Khám phá";
        else if ((m1 == "SURPRISED" && m2 == "CALM") || (m1 == "CALM" && m2 == "SURPRISED"))
            return "Khám phá";
        else if ((m1 == "SURPRISED" && m2 == "CONFUSED") || (m1 == "CONFUSED" && m2 == "SURPRISED"))
            return "Khám phá";

        // --- 9. Mutual Calm (Yên tĩnh) ---
        else if (m1 == "CALM" && m2 == "CALM")
            return "Yên tĩnh";
        else if ((m1 == "CALM" && m2 == "CONFUSED") || (m1 == "CONFUSED" && m2 == "CALM"))
            return "Yên tĩnh";

        // --- 10. Low-Intimacy Boundary (Thư giãn) ---
        else if (m1 == "DISGUSTED" && m2 == "DISGUSTED")
            return "Thư giãn";
        else if ((m1 == "DISGUSTED" && m2 == "CALM") || (m1 == "CALM" && m2 == "DISGUSTED"))
            return "Thư giãn";
        else if ((m1 == "DISGUSTED" && m2 == "SURPRISED") || (m1 == "SURPRISED" && m2 == "DISGUSTED"))
            return "Thư giãn";
        else if ((m1 == "DISGUSTED" && m2 == "FEAR") || (m1 == "FEAR" && m2 == "DISGUSTED"))
            return "Thư giãn";
        else if ((m1 == "DISGUSTED" && m2 == "CONFUSED") || (m1 == "CONFUSED" && m2 == "DISGUSTED"))
            return "Thư giãn";
        else if ((m1 == "DISGUSTED" && m2 == "ANGRY") || (m1 == "ANGRY" && m2 == "DISGUSTED"))
            return "Thư giãn";

        // --- 11. Stress and Tension (Cân bằng) ---
        else if (m1 == "ANGRY" && m2 == "ANGRY")
            return "Cân bằng";
        else if ((m1 == "ANGRY" && m2 == "CALM") || (m1 == "CALM" && m2 == "ANGRY"))
            return "Cân bằng";
        else if ((m1 == "ANGRY" && m2 == "FEAR") || (m1 == "FEAR" && m2 == "ANGRY"))
            return "Cân bằng";
        else if (m1 == "FEAR" && m2 == "FEAR")
            return "Cân bằng";
        else if ((m1 == "FEAR" && m2 == "CONFUSED") || (m1 == "CONFUSED" && m2 == "FEAR"))
            return "Cân bằng";

        // --- 12. Neutral / Mixed Mood (Trung lập) ---
        else if (m1 == "CONFUSED" && m2 == "CONFUSED")
            return "Trung lập";

        // Fallback mặc định
        else
            return "Trung lập";
    }
}
