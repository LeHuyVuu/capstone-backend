namespace capstone_backend.Business.DTOs.QuestionAnswer
{
    public class QuestionAnswerResponse
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public int? OrderIndex { get; set; }
        public string AnswerContent { get; set; } = null!;
        public string? ScoreKey { get; set; }
        public int? ScoreValue { get; set; }        
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool? IsActive { get; set; }
    }
}
