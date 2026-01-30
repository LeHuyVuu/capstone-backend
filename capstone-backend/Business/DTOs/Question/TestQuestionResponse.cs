using capstone_backend.Business.DTOs.QuestionAnswer;

namespace capstone_backend.Business.DTOs.Question
{
    public class TestQuestionResponse
    {
        public int QuestionId { get; set; }
        public string Content { get; set; } = null!;
        public int OrderIndex { get; set; }

        public List<TestAnswerOptionDto> Options { get; set; } = [];
    }
}
