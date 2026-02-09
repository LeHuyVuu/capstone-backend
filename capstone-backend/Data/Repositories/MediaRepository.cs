using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;

namespace capstone_backend.Data.Repositories
{
    public class MediaRepository : GenericRepository<Media>, IMediaRepository
    {
        public MediaRepository(MyDbContext context) : base(context)
        {
        }
    }
}
