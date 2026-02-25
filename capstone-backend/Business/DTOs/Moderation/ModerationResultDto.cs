namespace capstone_backend.Business.DTOs.Moderation
{
    public enum ModerationAction
    {
        PASS,
        BLOCK,
        PENDING
    }

    public class ModerationResultDto
    {
        public string Label { get; set; } = string.Empty;
        public ModerationAction Action { get; set; }
        public string Reason { get; set; } = string.Empty;

        public static ModerationResultDto Safe(string label)
            => new() { Label = label, Action = ModerationAction.PASS };
        public static ModerationResultDto Block(string label, string r)
            => new() { Label = label, Action = ModerationAction.BLOCK, Reason = r };

        public static ModerationResultDto NeedReview(string label, string r)
            => new() { Label = label, Action = ModerationAction.PENDING, Reason = r };
    }
}
