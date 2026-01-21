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

        public async Task<TestType?> GetByNameAsync(string name)
        {
            Console.WriteLine(name);
            return await _dbSet.FirstOrDefaultAsync(t => t.Name == name);
        }
    }
}
