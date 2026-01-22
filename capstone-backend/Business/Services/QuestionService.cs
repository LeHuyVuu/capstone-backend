using capstone_backend.Business.DTOs.Question;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace capstone_backend.Business.Services
{
    public class QuestionService : IQuestionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public QuestionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ImportResult> GenerateQuestionAsync(int testTypeId, Stream csvStream, CancellationToken ct = default)
        {
            try
            {
                // 1. Read CSV rows
                var rows = CsvReader(csvStream);

                // 2. Validate CSV rows
                var errors = ValidateCsvRows(rows);
                if (errors.Any())
                {
                    return new ImportResult(
                        rows.Count,
                        0,
                        errors
                    );
                }

                // 3. Get current max order if already have questions of this test type
                var order = await _unitOfWork.Questions.GetCurrentMaxOrderAsync(testTypeId, ct);

                var inserted = 0;

                // 4. Generate questions
                foreach (var row in rows)
                {
                    ct.ThrowIfCancellationRequested();

                    var (scoreA, scoreB) = GetScoreKey(row.Dimension);

                    var question = new Question
                    {
                        TestTypeId = testTypeId,
                        Content = row.Question.Trim(),
                        AnswerType = row.Type.Trim(),
                        OrderIndex = ++order,
                        Dimension = row.Dimension.Trim().ToUpperInvariant(),
                    };

                    var answer1 = new QuestionAnswer
                    {
                        Question = question,
                        AnswerContent = row.Answer1.Trim(),
                        ScoreKey = scoreA,
                        ScoreValue = 1,
                        OrderIndex = 1,
                    };

                    var answer2 = new QuestionAnswer
                    {
                        Question = question,
                        AnswerContent = row.Answer2.Trim(),
                        ScoreKey = scoreB,
                        ScoreValue = 1,
                        OrderIndex = 2,
                    };

                    // 5. Save to database
                    await _unitOfWork.Questions.AddAsync(question);
                    await _unitOfWork.QuestionAnswers.AddRangeAsync(new[] { answer1, answer2 });

                    inserted++;
                }

                await _unitOfWork.SaveChangesAsync();
                
                return new ImportResult(
                    rows.Count,
                    inserted,
                    Array.Empty<string>()
                );
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        private static List<QuestionImportRow> CsvReader(Stream csvStream)
        {
            csvStream.Position = 0;

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                HasHeaderRecord = true,
                PrepareHeaderForMatch = args =>
                    args.Header.Trim().Trim('\uFEFF').ToLowerInvariant()
            };

            using var reader = new StreamReader(csvStream, leaveOpen: true);
            using var csv = new CsvReader(reader, config);

            return csv.GetRecords<QuestionImportRow>().ToList();
        }

        private static List<string> ValidateCsvRows(List<QuestionImportRow> rows)
        {
            var errors = new List<string>();
            var allowed = new[] { "E/I", "S/N", "T/F", "J/P" };

            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var line = i + 2;

                if (!allowed.Contains(r.Dimension?.Trim().ToUpperInvariant()))
                    errors.Add($"Line {line}: invalid dimension");

                if (string.IsNullOrEmpty(r.Type))
                    errors.Add($"Line {line}: type is empty");

                if (string.IsNullOrEmpty(r.Question))
                    errors.Add($"Line {line}: question is empty");

                if (string.IsNullOrEmpty(r.Answer1))
                    errors.Add($"Line {line}: answer 1 is empty");

                if (string.IsNullOrEmpty(r.Answer2))
                    errors.Add($"Line {line}: answer 2 is empty");
            }

            return errors;
        }

        private static (string, string) GetScoreKey(string dimension)
        {
            return dimension.ToUpperInvariant() switch
            {
                "E/I" => ("E", "I"),
                "S/N" => ("S", "N"),
                "T/F" => ("T", "F"),
                "J/P" => ("J", "P"),
                _ => throw new ArgumentException("Invalid dimension"),
            };
        }
    }
}
