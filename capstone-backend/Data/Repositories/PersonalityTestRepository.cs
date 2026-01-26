using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories
{
    public class PersonalityTestRepository : GenericRepository<PersonalityTest>, IPersonalityTestRepository
    {
        public PersonalityTestRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<PersonalityTest?> GetByMemberAndTestTypeAsync(int memberId, int testTypeId, string status)
        {
            return await _dbSet
                .Where(pt => pt.MemberId == memberId && pt.TestTypeId == testTypeId && pt.IsDeleted == false && pt.Status == status)
                .FirstOrDefaultAsync();
        }
    }
}
