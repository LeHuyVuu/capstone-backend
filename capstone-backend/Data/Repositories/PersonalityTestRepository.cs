using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;

namespace capstone_backend.Data.Repositories
{
    public class PersonalityTestRepository : GenericRepository<PersonalityTest>, IPersonalityTestRepository
    {
        public PersonalityTestRepository(MyDbContext context) : base(context)
        {
        }
    }
}
