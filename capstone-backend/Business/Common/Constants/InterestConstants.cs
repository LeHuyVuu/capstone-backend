namespace capstone_backend.Business.Common.Constants
{
    public static class InterestConstants
    {
        public static readonly List<InterestMetadata> All = new()
        {
            new() { Key = "date-food", Display = "Hẹn hò & Ăn uống", Icon = "🍔" },
            new() { Key = "deep-talk", Display = "Tâm sự & Chữa lành", Icon = "🌿" },
            new() { Key = "memories", Display = "Kỷ niệm & Nhật ký", Icon = "📸" },
            new() { Key = "love-tips", Display = "Kiến thức & Gắn kết", Icon = "💡" },
            new() { Key = "experiences", Display = "Trải nghiệm chung", Icon = "✈️" }
        };
    }

    public class InterestMetadata
    {
        public string Key { get; set; }
        public string Display { get; set; }
        public string Icon { get; set; }
    }
}
