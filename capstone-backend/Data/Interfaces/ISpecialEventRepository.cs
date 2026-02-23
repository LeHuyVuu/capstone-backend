using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces;

public interface ISpecialEventRepository : IGenericRepository<SpecialEvent>
{
    Task<List<SpecialEvent>> GetActiveSpecialEventsAsync();
}
