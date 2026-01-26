using AutoMapper;
using AutoMapper.Execution;
using Azure.Core;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.PersonalityTest;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Data.Static;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.OpenApi.Models;
using System.Globalization;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace capstone_backend.Business.Services
{
    public class PersonalityTestService : IPersonalityTestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public PersonalityTestService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<PagedResult<PersonalityTestResponse>> GetHistoryTests(int pageNumber, int pageSize, int userId)
        {
            try
            {
                var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
                if (member == null)
                    throw new Exception("Member profile not found");

                var (tests, count) = await _unitOfWork.PersonalityTests.GetPagedAsync(pageNumber, pageSize, filter: (pt => pt.MemberId == member.Id && pt.IsDeleted == false), orderBy: (pt => pt.OrderByDescending(pt => pt.TakenAt)));

                var toMap = tests.ToList();

                var result = _mapper.Map<List<PersonalityTestResponse>>(toMap);

                return new PagedResult<PersonalityTestResponse>
                {
                    Items = result,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = count
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<int> HandleTestAsync(int userId, int testTypeId, SaveTestResultRequest request)
        {
            try
            {
                // Check member existence
                var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
                if (member == null)
                    throw new Exception("Member profile not found");

                // Check test type existence
                var testType = await _unitOfWork.TestTypes.GetByIdForMemberAsync(testTypeId);
                if (testType == null)
                    throw new Exception("Test type not found");

                // Validate answers
                if (request.Answers != null && request.Answers.Any())
                {
                    if (!await ValidateAnswers(request, testTypeId))
                        throw new Exception("Invalid answers submitted");
                }

                // Check if the member has already taken the test
                var record = await _unitOfWork.PersonalityTests.GetByMemberAndTestTypeAsync(member.Id, testTypeId, PersonalityTestStatus.IN_PROGRESS.ToString());
                if (record == null)
                    record = await CreateNewRecord(member.Id, testTypeId);

                var json = string.IsNullOrEmpty(record.ResultData)
                    ? new JsonObject()
                    : JsonNode.Parse(record.ResultData)!.AsObject();

                // MERGE NEW ANSWERS
                if (request.Answers != null && request.Answers.Any())
                {
                    MergeAnswers(json, request.Answers);
                }

                switch (request.Action)
                {
                    case TestAction.SAVE_PROGRESS:
                        // Update result data
                        json["status"] = PersonalityTestStatus.IN_PROGRESS.ToString();
                        if (request.CurrentQuestionIndex.HasValue)
                            json["currentQuestionIndex"] = request.CurrentQuestionIndex.Value;
                        break;

                    case TestAction.SUBMIT:
                        // Update result data
                        record.ResultCode = await HandleSubmitAsync(json, testTypeId, testType.TotalQuestions.Value);
                        record.Status = PersonalityTestStatus.COMPLETED.ToString();
                        record.TakenAt = DateTime.UtcNow;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException("Invalid action");
                }

                record.ResultData = json.ToJsonString();
                if (record.Id > 0)
                    _unitOfWork.PersonalityTests.Update(record);

                await _unitOfWork.SaveChangesAsync();
                return record.Id;
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        private async Task<PersonalityTest> CreateNewRecord(int memberId, int testTypeId)
        {
            var completedTest = await _unitOfWork.PersonalityTests.GetByMemberAndTestTypeAsync(memberId, testTypeId, PersonalityTestStatus.COMPLETED.ToString());
            if (completedTest != null)
            {
                completedTest.Status = PersonalityTestStatus.ARCHIVED.ToString();
                _unitOfWork.PersonalityTests.Update(completedTest);

            }

            var newRecord = new PersonalityTest
            {
                MemberId = memberId,
                TestTypeId = testTypeId,
                Status = PersonalityTestStatus.IN_PROGRESS.ToString(),
                ResultData = "{}"
            };

            await _unitOfWork.PersonalityTests.AddAsync(newRecord);
            return newRecord;
        }

        private void MergeAnswers(JsonObject json, List<AnswerDto> newAnswers)
        {
            var answersNode = json["answers"];
            JsonArray answersArray;

            if (answersNode is JsonArray arr)
                answersArray = arr;
            else
            {
                answersArray = new JsonArray();
                json["answers"] = answersArray;
            }

            foreach (var newAns in newAnswers)
            {
                var existingNode = answersArray
                    .FirstOrDefault(x => x?["QuestionId"]?.GetValue<int>() == newAns.QuestionId);

                // If found, update the answer
                if (existingNode != null)
                    answersArray.Remove(existingNode);
                answersArray.Add(JsonNode.Parse(JsonSerializer.Serialize(newAns)));
            }
        }

        private async Task<string> HandleSubmitAsync(JsonObject json, int testTypeId, int totalQuestions)
        {
            // 1. Extract answers
            var answerIds = new List<int>();
            if (json["answers"] is JsonArray arr)
            {
                foreach (var node in arr)
                {
                    var id = node?["AnswerId"]?.GetValue<int>();
                    if (id.HasValue)
                        answerIds.Add(id.Value);
                }
            }

            if (!answerIds.Any())
                throw new Exception("No answers to submit");

            if (answerIds.Count < totalQuestions)
                throw new Exception($"Not all questions have been answered: {answerIds.Count}/{totalQuestions}");

            // 2. Load score mapping from DB
            var scoringMap = await _unitOfWork.QuestionAnswers.GetScoringMapAsync(testTypeId);

            // 3. Calculate total score
            var scores = scoringMap
                .Where(x => answerIds.Contains(x.AnswerId))
                .GroupBy(x => x.ScoreKey)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.ScoreValue));

            int GetScore(string scoreKey) => scores.TryGetValue(scoreKey, out int val) ? val : 0;
            double GetPercent(int score1, int score2)
            {
                int total = score1 + score2;
                return total == 0 ? 0 : Math.Round((double)score1 / total * 100, 1);
            }

            // 4. Compare to get result type
            var sE = GetScore("E"); var sI = GetScore("I");
            var sS = GetScore("S"); var sN = GetScore("N");
            var sT = GetScore("T"); var sF = GetScore("F");
            var sJ = GetScore("J"); var sP = GetScore("P");

            var axis1 = sE >= sI ? "E" : "I";
            var axis2 = sS >= sN ? "S" : "N";
            var axis3 = sT >= sF ? "T" : "F";
            var axis4 = sJ >= sP ? "J" : "P";

            // Combine
            var finalMbtiCode = $"{axis1}{axis2}{axis3}{axis4}";
            var profile = MbtiContentStore.GetProfile(finalMbtiCode);

            json["status"] = PersonalityTestStatus.COMPLETED.ToString();
            json["completedAt"] = DateTime.UtcNow;
            json["result"] = new JsonObject
            {
                ["mbtiCode"] = finalMbtiCode,
                ["name"] = profile?.Name,
                ["description"] = profile?.Description != null
                    ? new JsonArray(profile.Description.Select(d => JsonValue.Create(d)).ToArray())
                    : new JsonArray(),
                ["breakdown"] = new JsonObject
                {
                    ["scores"] = new JsonObject
                    {
                        ["E"] = sE,
                        ["I"] = sI,
                        ["S"] = sS,
                        ["N"] = sN,
                        ["T"] = sT,
                        ["F"] = sF,
                        ["J"] = sJ,
                        ["P"] = sP
                    },

                    ["percent"] = new JsonObject
                    {
                        ["E_percent"] = GetPercent(sE, sI),
                        ["I_percent"] = GetPercent(sI, sE),
                        ["S_percent"] = GetPercent(sS, sN),
                        ["N_percent"] = GetPercent(sN, sS),
                        ["T_percent"] = GetPercent(sT, sF),
                        ["F_percent"] = GetPercent(sF, sT),
                        ["J_percent"] = GetPercent(sJ, sP),
                        ["P_percent"] = GetPercent(sP, sJ),
                    }
                },
            };

            return finalMbtiCode;
        }

        private async Task<bool> ValidateAnswers(SaveTestResultRequest request, int testTypeId)
        {
            if (request.Answers == null || !request.Answers.Any())
                return false;

            // 1. Check for duplicate question IDs
            var answerIdsToCheck = new HashSet<int>();
            var questionIdsToCheck = new HashSet<int>();
            foreach (var ans in request.Answers)
            {
                if (!questionIdsToCheck.Add(ans.QuestionId))
                    return false;

                answerIdsToCheck.Add(ans.AnswerId);
            }

            var validMap = await _unitOfWork.Questions.GetValidStructureAsync(testTypeId);
            foreach (var ans in request.Answers)
            {
                // 2. Check question IDs belong to the test type
                if (!validMap.ContainsKey(ans.QuestionId))
                    return false;

                // 3. Check answer IDs belong to the question IDs
                var validAnswersForQuestion = validMap[ans.QuestionId];
                if (!validAnswersForQuestion.Contains(ans.AnswerId))
                    return false;
            }

            return true;
        }
    }
}
