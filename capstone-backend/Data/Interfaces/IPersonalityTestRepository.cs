using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces
{
    public interface IPersonalityTestRepository : IGenericRepository<PersonalityTest>
    {
        Task<PersonalityTest?> GetByMemberAndTestTypeAsync(int memberId, int testTypeId, string status);
    }
}
