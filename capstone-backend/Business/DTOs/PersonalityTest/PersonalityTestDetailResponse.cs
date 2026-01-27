namespace capstone_backend.Business.DTOs.PersonalityTest
{
    public class PersonalityTestDetailResponse : PersonalityTestResponse
    {
        public object? Summary { get; set; }
        public List<MemberAnswerDetailDto> Details { get; set; }
    }

    public class MemberAnswerDetailDto
    {
        public int QuestionId { get; set; }
        public string QuestionContent { get; set; }
        public int MemberSelectedAnswerId { get; set; }
        public List<AnswerOptionDto> Options { get; set; }
    }

    public class AnswerOptionDto
    {
        public int AnswerId { get; set; }
        public string Content { get; set; }
        public string ScoreKey { get; set; } // "E", "I"...
        public int ScoreValue { get; set; }
        public bool IsSelected { get; set; }
    }
}
