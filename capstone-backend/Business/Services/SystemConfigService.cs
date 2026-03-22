using AutoMapper;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.SystemConfig;
using capstone_backend.Business.Interfaces;

namespace capstone_backend.Business.Services
{
    public class SystemConfigService : ISystemConfigService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SystemConfigService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<PagedResult<SystemConfigResponse>> GetAllConfigsAsync(int pageNumber, int pageSize)
        {
            var (configs, totalCount) = await _unitOfWork.SystemConfigs.GetPagedAsync(
                pageNumber,
                pageSize,
                sc => sc.IsDeleted == false,
                sc => sc.OrderByDescending(s => s.UpdatedAt)
            );

            var response = _mapper.Map<List<SystemConfigResponse>>(configs);
            return new PagedResult<SystemConfigResponse>
            {
                Items = response,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
    }
}
