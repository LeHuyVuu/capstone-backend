using AutoMapper;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.SystemConfig;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;

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

        public async Task<SystemConfigResponse> UpdateConfigAsync(UpdateSystemConfigRequest request)
        {
            var key = request.ConfigKey.Trim().ToUpper();
            var value = request.ConfigValue.Trim();

            var config = await _unitOfWork.SystemConfigs.GetByKeyAsync(key);
            if (config == null)
                throw new Exception("Không tìm thấy config");

            ValidateConfig(key, value);

            config.ConfigValue = value;
            config.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.SystemConfigs.Update(config);
            await _unitOfWork.SaveChangesAsync();

            var response = _mapper.Map<SystemConfigResponse>(config);
            return response;
        }

        private static void ValidateConfig(string key, string value)
        {
            switch (key)
            {
                case "MONEY_TO_POINT_RATE":
                    if (!int.TryParse(value, out var rate) || rate <= 0)
                        throw new Exception("Tỉ lệ đổi tiền sang point phải là số nguyên > 0");
                    break;

                case "VENUE_COMMISSION_PERCENT":
                    if (!decimal.TryParse(value, out var percent) || percent < 0 || percent > 100)
                        throw new Exception("Phần trăm hoa hồng phải nằm trong khoảng từ 0 đến 100");
                    break;

                default:
                    throw new Exception("Config key không hợp lệ");
            }
        }
    }
}
