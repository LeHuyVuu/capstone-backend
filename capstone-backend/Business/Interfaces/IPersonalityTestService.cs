
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.PersonalityTest;

namespace capstone_backend.Business.Interfaces
{
    public interface IPersonalityTestService
    {
        Task<int> HandleTestAsync(int userId, int testTypeId, SaveTestResultRequest request);
        Task<PagedResult<PersonalityTestResponse>> GetHistoryTests(int pageNumber, int pageSize, int userId);
        Task<PersonalityTestDetailResponse> GetTestHistoryDetailAsync(int id, int userId);
        Task<PersonalityTestResponse> GetCurrentPersonalityAsync(int userId);
    }
}
