using capstone_backend.Business.DTOs.TestType;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Interfaces
{
    public interface ITestTypeService
    {
        Task<List<TestTypeResponse>> GetAllTestTypeAsync();
        Task<TestTypeDetailDto?> GetByIdAsync(int id);
        Task<int> CreateTestTypeAsync(CreateTestTypeResquest request);
        Task<int> UpdateTestTypeAsync(int id, UpdateTestTypeRequest request);
        Task<int> DeleteTestTypeAsync(int id);
    }
}
