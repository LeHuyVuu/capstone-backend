namespace capstone_backend.Business.Common.Constants
{
    public static class InterestConstants
    {
        public static readonly List<InterestMetadata> All = new()
        {
            new("date-food", "Hẹn hò & Ăn uống", "🍔", new[] { "Đi ăn", "Cafe", "Nhậu", "Tự nấu" }),
            new("deep-talk", "Tâm sự & Chữa lành", "🌿", new[] { "Trải lòng", "Nghe Podcast", "Đọc sách", "Chữa lành" }),
            new("memories", "Kỷ niệm & Nhật ký", "📸", new[] { "Chụp ảnh", "Quay phim", "Viết nhật ký", "Tặng quà" }),
            new("love-tips", "Kiến thức & Gắn kết", "💡", new[] { "Xem mẹo yêu", "Hiểu đối phương", "Giải quyết cãi vã" }),
            new("experiences", "Trải nghiệm chung", "✈️", new[] { "Du lịch", "Xem phim", "Camping", "Thể thao" })
        };

        public static string GetParent(string childDisplay) =>
            All.FirstOrDefault(x => x.Children.Contains(childDisplay))?.Key ?? "others";
    }

    public record InterestMetadata(string Key, string Display, string Icon, string[] Children);
}
