using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces
{
    public interface ITestTypeRepository : IGenericRepository<TestType>
    {
        public Task<TestType?> GetByNameAsync(string name);
        Task<TestType?> GetByIdForMemberAsync(int id);
    }
}
