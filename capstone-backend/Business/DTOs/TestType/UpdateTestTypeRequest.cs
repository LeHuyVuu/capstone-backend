namespace capstone_backend.Business.DTOs.TestType
{
    public class UpdateTestTypeRequest
    {
        /// <example>multipleChoice</example>
        public string? Name { get; set; }
        /// <example>description</example>
        public string? Description { get; set; }
        /// <example>20</example>
        public int? TotalQuestions { get; set; }
    }
}
