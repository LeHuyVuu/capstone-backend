namespace capstone_backend.Business.DTOs.Question
{
    public sealed class QuestionImportRow
    {
        public string Dimension { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string Question { get; set; } = null!;
        public string Answer1 { get; set; } = null!;
        public string Answer2 { get; set; } = null!;
    }

    public sealed record ImportResult(
        int TotalRows,
        int InsertedQuestions,
        IReadOnlyList<string> Errors
    );
}
