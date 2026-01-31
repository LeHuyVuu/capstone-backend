using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace capstone_backend.Data.Repositories
{
    public class PersonalityTestRepository : GenericRepository<PersonalityTest>, IPersonalityTestRepository
    {
        public PersonalityTestRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<PersonalityTest>> GetAllAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Where(pt => pt.IsDeleted == false)
                .ToListAsync();
        }

        public async Task<PersonalityTest?> GetByIdAndMemberIdAsync(int id, int memberId)
        {
            return await _dbSet.Where(pt => pt.Id == id && pt.MemberId == memberId && pt.IsDeleted == false).FirstOrDefaultAsync();
        }

        public async Task<PersonalityTest?> GetByMemberAndTestTypeAsync(int memberId, int testTypeId, string status)
        {
            return await _dbSet
                .Where(pt => pt.MemberId == memberId && pt.TestTypeId == testTypeId && pt.IsDeleted == false && pt.Status == status)
                .FirstOrDefaultAsync();
        }

        public Task<PersonalityTest?> GetCurrentPersonalityAsync(int memberId)
        {
            return _dbSet
                .AsNoTracking()
                .Where(pt => pt.MemberId == memberId && pt.IsDeleted == false && pt.Status == PersonalityTestStatus.COMPLETED.ToString())
                .OrderByDescending(pt => pt.TakenAt ?? pt.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public Task<PersonalityTest?> GetInProgressTestByUserAndTestTypeAsync(int memberId, int testTypeId)
        {
            return _dbSet
                .Where(pt => pt.MemberId == memberId && pt.TestTypeId == testTypeId && pt.IsDeleted == false && pt.Status == PersonalityTestStatus.IN_PROGRESS.ToString())
                .FirstOrDefaultAsync();
        }
    }
}
