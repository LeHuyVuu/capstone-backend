using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.SpecialEvent;

namespace capstone_backend.Business.Interfaces;

public interface ISpecialEventService
{
    Task<SpecialEventResponse> CreateSpecialEventAsync(CreateSpecialEventRequest request, CancellationToken cancellationToken = default);
    Task<SpecialEventResponse?> GetSpecialEventByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<SpecialEventResponse>> GetAllSpecialEventsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<SpecialEventResponse>> GetActiveSpecialEventsAsync(CancellationToken cancellationToken = default);
    Task<SpecialEventResponse?> UpdateSpecialEventAsync(int id, UpdateSpecialEventRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteSpecialEventAsync(int id, CancellationToken cancellationToken = default);
}
