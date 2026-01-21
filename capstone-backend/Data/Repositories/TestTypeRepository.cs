using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;

namespace capstone_backend.Data.Repositories
{
    public class TestTypeRepository : GenericRepository<TestType>, ITestTypeRepository
    {
        public TestTypeRepository(MyDbContext context) : base(context)
        {
        }
    }
}
