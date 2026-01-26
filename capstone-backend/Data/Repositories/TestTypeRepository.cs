using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories
{
    public class TestTypeRepository : GenericRepository<TestType>, ITestTypeRepository
    {
        public TestTypeRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<TestType>> GetAllAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Where(t => t.IsDeleted == false)
                .ToListAsync();
        }

        public async Task<TestType?> GetByIdAsync(int id)
        {
            return await _dbSet
                .FirstOrDefaultAsync(t => t.Id == id && t.IsDeleted == false);
        }

        public async Task<TestType?> GetByIdForMemberAsync(int id)
        {
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id && t.IsDeleted == false && t.IsActive == true);
        }

        public async Task<TestType?> GetByNameAsync(string name)
        {
            return await _dbSet.FirstOrDefaultAsync(t => t.Name == name);
        }
    }
}
