namespace capstone_backend.Data.Static
{
    public static class MbtiContentStore
    {
        public class MbtiProfileInfo
        {
            public string Code { get; set; }
            public string Name { get; set; }
            public List<string> Description { get; set; }
            public string PersonalityDescription { get; set; }
        }

        private static readonly Dictionary<string, MbtiProfileInfo> _profiles = new()
        {
            ["ISTJ"] = new MbtiProfileInfo
            {
                Code = "ISTJ",
                Name = "Người Trách Nhiệm",
                Description = new()
                {
                    "Sống nguyên tắc, thực tế và đáng tin cậy.",
                    "Luôn coi trọng kỷ luật, trách nhiệm và sự ổn định.",
                    "Làm việc có kế hoạch, tuân thủ quy trình rõ ràng.",
                    "Không thích mơ mộng, ưu tiên hiệu quả và kết quả thực tế."
                },
                PersonalityDescription = "responsible, practical, disciplined, reliable, organized, realistic"
            },

            ["ISFJ"] = new MbtiProfileInfo
            {
                Code = "ISFJ",
                Name = "Người Bảo Vệ",
                Description = new()
                {
                    "Ân cần, chu đáo và quan tâm đến người khác.",
                    "Luôn âm thầm hỗ trợ và bảo vệ những người xung quanh.",
                    "Coi trọng truyền thống và sự an toàn.",
                    "Làm việc bền bỉ, ít phô trương."
                },
                PersonalityDescription = "caring, kind, supportive, loyal, careful, dependable"
            },

            ["INFJ"] = new MbtiProfileInfo
            {
                Code = "INFJ",
                Name = "Người Cố Vấn",
                Description = new()
                {
                    "Sâu sắc, lý tưởng và có tầm nhìn dài hạn.",
                    "Quan tâm mạnh mẽ đến giá trị và ý nghĩa cuộc sống.",
                    "Nhạy cảm với cảm xúc người khác.",
                    "Thường suy nghĩ nhiều và sống nội tâm."
                },
                PersonalityDescription = "deep, idealistic, thoughtful, caring, reflective, quiet"
            },

            ["INTJ"] = new MbtiProfileInfo
            {
                Code = "INTJ",
                Name = "Nhà Chiến Lược",
                Description = new()
                {
                    "Tư duy logic, độc lập và có chiến lược.",
                    "Luôn lên kế hoạch dài hạn và tối ưu hệ thống.",
                    "Ít bị cảm xúc chi phối khi ra quyết định.",
                    "Không thích sự mơ hồ, thiếu logic."
                },
                PersonalityDescription = "strategic, logical, independent, focused, organized, decisive"
            },

            ["ISTP"] = new MbtiProfileInfo
            {
                Code = "ISTP",
                Name = "Nhà Kỹ Thuật",
                Description = new()
                {
                    "Linh hoạt, thực tế và thích khám phá.",
                    "Giỏi xử lý tình huống phát sinh.",
                    "Ưa hành động hơn lời nói.",
                    "Không thích bị ràng buộc bởi quy tắc cứng nhắc."
                },
                PersonalityDescription = "practical, flexible, hands-on, calm, active, independent"
            },

            ["ISFP"] = new MbtiProfileInfo
            {
                Code = "ISFP",
                Name = "Người Nghệ Sĩ",
                Description = new()
                {
                    "Nhẹ nhàng, sống theo cảm xúc và giá trị cá nhân.",
                    "Yêu cái đẹp và sự tự do.",
                    "Không thích xung đột hay áp lực.",
                    "Thường thể hiện bản thân qua hành động hơn lời nói."
                },
                PersonalityDescription = "gentle, sensitive, free-spirited, artistic, peaceful, genuine"
            },

            ["INFP"] = new MbtiProfileInfo
            {
                Code = "INFP",
                Name = "Người Lý Tưởng Hóa",
                Description = new()
                {
                    "Nhạy cảm, giàu lòng trắc ẩn.",
                    "Sống theo lý tưởng và giá trị cá nhân.",
                    "Quan tâm đến ý nghĩa sâu xa của cuộc sống.",
                    "Dễ bị tổn thương nhưng rất chân thành."
                },
                PersonalityDescription = "idealistic, caring, sensitive, sincere, thoughtful, introspective"
            },

            ["INTP"] = new MbtiProfileInfo
            {
                Code = "INTP",
                Name = "Nhà Tư Duy",
                Description = new()
                {
                    "Tò mò, logic và thích phân tích.",
                    "Luôn tìm cách hiểu bản chất vấn đề.",
                    "Không thích những quy tắc vô lý.",
                    "Thường đắm chìm trong suy nghĩ."
                },
                PersonalityDescription = "analytical, curious, logical, thoughtful, independent, reflective"
            },

            ["ESTP"] = new MbtiProfileInfo
            {
                Code = "ESTP",
                Name = "Người Năng Động",
                Description = new()
                {
                    "Nhiệt huyết, táo bạo và thích hành động.",
                    "Giỏi ứng biến và nắm bắt cơ hội.",
                    "Không thích chờ đợi hay lý thuyết dài dòng.",
                    "Thích trải nghiệm thực tế."
                },
                PersonalityDescription = "energetic, bold, active, adaptable, practical, spontaneous"
            },

            ["ESFP"] = new MbtiProfileInfo
            {
                Code = "ESFP",
                Name = "Người Trình Diễn",
                Description = new()
                {
                    "Vui vẻ, hòa đồng và tràn đầy năng lượng.",
                    "Thích trở thành trung tâm của sự chú ý.",
                    "Sống cho hiện tại và trải nghiệm.",
                    "Dễ tạo cảm giác thoải mái cho người khác."
                },
                PersonalityDescription = "cheerful, friendly, expressive, energetic, fun, social"
            },

            ["ENFP"] = new MbtiProfileInfo
            {
                Code = "ENFP",
                Name = "Người Truyền Cảm Hứng",
                Description = new()
                {
                    "Nhiệt tình, sáng tạo và giàu cảm xúc.",
                    "Luôn nhìn thấy tiềm năng ở người khác.",
                    "Yêu tự do và những ý tưởng mới.",
                    "Dễ chán khi bị gò bó."
                },
                PersonalityDescription = "enthusiastic, creative, emotional, inspiring, curious, free-spirited"
            },

            ["ENTP"] = new MbtiProfileInfo
            {
                Code = "ENTP",
                Name = "Nhà Phát Minh",
                Description = new()
                {
                    "Thông minh, linh hoạt và thích tranh luận.",
                    "Luôn tìm ra hướng đi mới.",
                    "Không thích sự lặp lại nhàm chán.",
                    "Giỏi thuyết phục và kết nối ý tưởng."
                },
                PersonalityDescription = "smart, curious, innovative, talkative, persuasive, adaptable"
            },

            ["ESTJ"] = new MbtiProfileInfo
            {
                Code = "ESTJ",
                Name = "Người Điều Hành",
                Description = new()
                {
                    "Quyết đoán, thực tế và có tổ chức.",
                    "Giỏi quản lý và lãnh đạo.",
                    "Đề cao kỷ luật và hiệu suất.",
                    "Không thích sự thiếu rõ ràng."
                },
                PersonalityDescription = "decisive, organized, practical, disciplined, direct, efficient"
            },

            ["ESFJ"] = new MbtiProfileInfo
            {
                Code = "ESFJ",
                Name = "Người Chăm Sóc",
                Description = new()
                {
                    "Thân thiện, trách nhiệm và quan tâm cộng đồng.",
                    "Luôn mong muốn giúp đỡ người khác.",
                    "Coi trọng các mối quan hệ xã hội.",
                    "Nhạy cảm với sự đánh giá."
                },
                PersonalityDescription = "friendly, caring, helpful, social, responsible, supportive"
            },

            ["ENFJ"] = new MbtiProfileInfo
            {
                Code = "ENFJ",
                Name = "Người Dẫn Dắt",
                Description = new()
                {
                    "Lôi cuốn, đồng cảm và truyền cảm hứng.",
                    "Giỏi kết nối và dẫn dắt tập thể.",
                    "Quan tâm đến sự phát triển của người khác.",
                    "Dễ ôm đồm trách nhiệm."
                },
                PersonalityDescription = "charismatic, caring, inspiring, social, supportive, responsible"
            },

            ["ENTJ"] = new MbtiProfileInfo
            {
                Code = "ENTJ",
                Name = "Nhà Lãnh Đạo",
                Description = new()
                {
                    "Quyết đoán, chiến lược và định hướng mục tiêu.",
                    "Giỏi tổ chức và điều phối nguồn lực.",
                    "Không ngại thử thách lớn.",
                    "Đôi khi bị xem là cứng rắn."
                },
                PersonalityDescription = "assertive, strategic, goal-focused, organized, confident, strong-willed"
            }
        };

        public static MbtiProfileInfo GetProfile(string code)
        {
            return _profiles.TryGetValue(code, out var profile)
                ? profile
                : new MbtiProfileInfo
                {
                    Code = code,
                    Name = "Unknown",
                    Description = new() { "Chưa có dữ liệu" }
                };
        }
    }
}
