using capstone_backend.Data.Enums;

namespace capstone_backend.Business.DTOs.PersonalityTest
{
    public class SaveTestResultRequest
    {
        public TestAction Action { get; set; }
        public int? CurrentQuestionIndex { get; set; }
        public List<AnswerDto> Answers { get; set; } = new();
    }
}
