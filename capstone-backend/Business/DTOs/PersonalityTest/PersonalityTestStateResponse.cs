namespace capstone_backend.Business.DTOs.PersonalityTest
{
    public class PersonalityTestStateResponse
    {
        public string State { get; set; } = null!;
        public int TestTypeId { get; set; }
        public int CurrentQuestionIndex { get; set; }
        public int AnsweredCount { get; set; }
        public int TotalQuestions { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
