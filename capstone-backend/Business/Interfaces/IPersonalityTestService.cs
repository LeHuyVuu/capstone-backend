
using capstone_backend.Business.DTOs.PersonalityTest;

namespace capstone_backend.Business.Interfaces
{
    public interface IPersonalityTestService
    {
        Task<int> HandleTestAsync(int userId, int testTypeId, SaveTestResultRequest request);
    }
}
