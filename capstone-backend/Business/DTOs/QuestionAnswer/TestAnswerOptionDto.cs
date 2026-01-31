namespace capstone_backend.Business.DTOs.QuestionAnswer
{
    public class TestAnswerOptionDto
    {
        public int AnswerId { get; set; }
        public string Content { get; set; } = null!;
        public string? ScoreKey { get; set; }
        public int? ScoreValue { get; set; }

        public bool IsSelected { get; set; }
    }
}
