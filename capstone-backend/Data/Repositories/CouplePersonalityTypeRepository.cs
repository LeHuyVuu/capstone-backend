using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories
{
    public class CouplePersonalityTypeRepository : GenericRepository<CouplePersonalityType>, ICouplePersonalityTypeRepository
    {
        public CouplePersonalityTypeRepository(MyDbContext context) : base(context)
        {
        }
    }
}
