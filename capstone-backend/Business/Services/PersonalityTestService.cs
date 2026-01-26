using AutoMapper.Execution;
using capstone_backend.Business.DTOs.PersonalityTest;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using CsvHelper;
using CsvHelper.Configuration;
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

        public PersonalityTestService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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

                // Check if the member has already taken the test
                var record = await _unitOfWork.PersonalityTests.GetByMemberAndTestTypeAsync(member.Id, testTypeId, PersonalityTestStatus.IN_PROGRESS.ToString());
                if (record == null)
                    record = await CreateNewRecord(member.Id, testTypeId);

                var json = string.IsNullOrEmpty(record.ResultData)
                    ? new JsonObject()
                    : JsonNode.Parse(record.ResultData)!.AsObject();

                switch (request.Action)
                {
                    case TestAction.SAVE_PROGRESS:
                        // Update result data
                        HandleSaveProgress(json, request);
                        break;

                    //case TestAction.SUBMIT:
                    //    // Update result data
                    //    HandleSubmit(json, request)
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

        private void HandleSaveProgress(JsonObject json, SaveTestResultRequest request)
        {
            json["status"] = PersonalityTestStatus.IN_PROGRESS.ToString();

            if (request.CurrentQuestionIndex.HasValue)
                json["currentQuestionIndex"] = request.CurrentQuestionIndex.Value;

            if (request.Answers != null)
            {
                var answersArray = new JsonArray();
                foreach (var ans in request.Answers)
                {
                    answersArray.Add(JsonNode.Parse(JsonSerializer.Serialize(ans)));
                }
                json["answers"] = answersArray;
            }

            json["result"] = null;
        }
    }
}
