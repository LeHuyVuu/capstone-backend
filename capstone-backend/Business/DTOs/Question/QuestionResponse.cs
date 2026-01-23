using capstone_backend.Business.DTOs.QuestionAnswer;

namespace capstone_backend.Business.DTOs.Question
{
    public class QuestionResponse
    {
        public int Id { get; set; }
        public int TestTypeId { get; set; }
        public int? OrderIndex { get; set; }
        public int? Version { get; set; }
        public string Content { get; set; } = null!;
        public string? AnswerType { get; set; }
        public string? Dimension { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool? IsActive { get; set; }
        public List<QuestionAnswerResponse>? Answers { get; set; }
    }
}
