using Amazon.Runtime.Internal;
using AutoMapper;
using capstone_backend.Api.Models;
using capstone_backend.Business.DTOs.Question;
using capstone_backend.Business.DTOs.QuestionAnswer;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace capstone_backend.Business.Services
{
    public class QuestionService : IQuestionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly S3StorageService _s3Service;
        private readonly ICurrentUser _currentUser;
        private readonly IMapper _mapper;

        public QuestionService(IUnitOfWork unitOfWork, S3StorageService s3Service, ICurrentUser currentUser, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _s3Service = s3Service;
            _currentUser = currentUser;
            _mapper = mapper;
        }

        public async Task<ImportResult> GenerateQuestionAsync(int testTypeId, IFormFile file, CancellationToken ct = default)
        {
            try
            {
                await using var csvStream = file.OpenReadStream();

                // 0. Validate test type ID
                var testType = await _unitOfWork.TestTypes.GetByIdAsync(testTypeId);
                if (testType == null)
                    throw new Exception("Test type not found");

                // 1. Read CSV rows
                var rows = CsvReader(csvStream);

                // 2. Validate CSV rows
                var errors = ValidateCsvRows(rows, testType.TotalQuestions.Value);
                if (errors.Any())
                {
                    return new ImportResult(
                        rows.Count,
                        0,
                        errors
                    );
                }

                // 3. Get current max order if already have questions of this test type
                var version = await _unitOfWork.Questions.GetCurrentVersionAsync(testTypeId, ct);
                var order = 0;
                var newVersion = version + 1;

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
                        Version = newVersion,
                        Dimension = row.Dimension.Trim().ToUpperInvariant(),
                        IsActive = true,
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

                var result = await _unitOfWork.SaveChangesAsync();

                // 6. Upload CSV to S3
                if (result > 0)
                {
                    var userId = _currentUser.UserId
                            ?? throw new UnauthorizedAccessException("User not authenticated");

                    await _s3Service.UploadFileAsync(file, userId, "DOCUMENT");
                }

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

        public async Task<int> ActivateVersionAsync(int testTypeId, int version)
        {
            var testType = await _unitOfWork.TestTypes.GetByIdAsync(testTypeId);
            if (testType == null)
                throw new Exception("Test type not found");

            var questions = await _unitOfWork.Questions.GetAllByVersionAsync(testTypeId, version);
            if (!questions.Any())
                throw new Exception("Test type has no questions");

            if (testType.CurrentVersion == version)
                throw new Exception("This version is already active");

            testType.CurrentVersion = version;

            _unitOfWork.TestTypes.Update(testType);
            return await _unitOfWork.SaveChangesAsync();
        }

        public async Task<List<QuestionResponse>> GetAllQuestionsByVersionAsync(int testTypeId, int version)
        {
            try
            {
                var testType = await _unitOfWork.TestTypes.GetByIdAsync(testTypeId);
                if (testType == null)
                    throw new Exception("Test type not found");

                // Check version existence
                var versions = await _unitOfWork.Questions.GetAllVersionsAsync(testTypeId);
                if (!versions.Any(v => v.Version == version))
                    throw new Exception("Version not found for this test type");

                var questions = await _unitOfWork.Questions.GetAllByVersionAsync(testTypeId, version);
                if (!questions.Any())
                    throw new Exception("Test type has no questions");

                var questionIds = questions.Select(q => q.Id).ToList();

                var questionResponses = _mapper.Map<List<QuestionResponse>>(questions);

                var answers = await _unitOfWork.QuestionAnswers
                        .GetAllByQuestionIdsAsync(questionIds);

                foreach (var q in questionResponses)
                {
                    q.Answers = answers
                        .Where(a => a.QuestionId == q.Id)
                        .Select(a => _mapper.Map<QuestionAnswerResponse>(a))
                        .ToList();
                }

                return questionResponses;
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<List<QuestionResponse>> GetAllQuestionsForMemberAsync(int testTypeId)
        {
            try
            {
                // Check test type existence
                var testType = await _unitOfWork.TestTypes.GetByIdAsync(testTypeId);
                if (testType == null)
                    throw new Exception("Test type not found");

                var version = testType.CurrentVersion;
                if (version == null)
                    throw new Exception("Test type has no active version");

                var questions = await _unitOfWork.Questions.GetAllQuestionsByTestTypeIdAsync(testTypeId);
                if (!questions.Any())
                    throw new Exception("Test type has no questions");

                var response = _mapper.Map<List<QuestionResponse>>(questions);

                return response;
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

        private static List<string> ValidateCsvRows(List<QuestionImportRow> rows, int totalQuestion)
        {
            var errors = new List<string>();

            if (totalQuestion <= 0)
                errors.Add("Expected totalQuestion must be > 0");

            if (rows == null || rows.Count == 0)
                return new List<string> { "CSV has no data rows" };

            if (rows.Count != totalQuestion)
                errors.Add($"Total questions in CSV ({rows.Count}) does not match expected ({totalQuestion})");

            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "E/I", "S/N", "T/F", "J/P" };

            var seenQuestions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var line = i + 2;

                // Check dimension
                var dim = r.Dimension?.Trim();
                if (string.IsNullOrWhiteSpace(dim) || !allowed.Contains(dim))
                    errors.Add($"Line {line}: invalid dimension");

                if (string.IsNullOrWhiteSpace(r.Type))
                    errors.Add($"Line {line}: type is empty");

                // Check question
                var q = r.Question?.Trim();
                if (string.IsNullOrWhiteSpace(q))
                    errors.Add($"Line {line}: question is empty");
                else
                {
                    if (!seenQuestions.Add(q))
                        errors.Add($"Line {line}: duplicate question");
                }

                // Check answers
                var a1 = r.Answer1?.Trim();
                var a2 = r.Answer2?.Trim();
                if (string.IsNullOrWhiteSpace(a1))
                    errors.Add($"Line {line}: answer 1 is empty");

                if (string.IsNullOrWhiteSpace(a2))
                    errors.Add($"Line {line}: answer 2 is empty");

                if (!string.IsNullOrWhiteSpace(a1) && !string.IsNullOrWhiteSpace(a2) &&
                     string.Equals(a1, a2, StringComparison.OrdinalIgnoreCase))
                    errors.Add($"Line {line}: answer 1 and answer 2 must be different");
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
