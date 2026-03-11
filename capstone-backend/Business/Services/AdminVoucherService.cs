using AutoMapper;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.Voucher;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Services
{
    public class AdminVoucherService : IAdminVoucherService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AdminVoucherService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<PagedResult<AdminVoucherDetailResponse>> GetAdminVouchersAsync(GetAdminVouchersRequest query)
        {
            int pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
            int pageSize = query.PageSize < 1 ? 10 : query.PageSize;

            var keyword = query.Keyword?.Trim().ToLower();

            // Create order ef
            Func<IQueryable<Voucher>, IOrderedQueryable<Voucher>> orderBy = q =>
                q.OrderByDescending(x => x.CreatedAt);

            if (!string.IsNullOrWhiteSpace(query.SortBy))
            {
                var sortBy = query.SortBy.Trim().ToLower();
                var order = query.OrderBy?.Trim().ToLower() ?? "desc";

                orderBy = (sortBy, order) switch
                {
                    ("createdat", "asc") => q => q.OrderBy(x => x.CreatedAt),
                    ("createdat", "desc") => q => q.OrderByDescending(x => x.CreatedAt),
                    ("updatedat", "asc") => q => q.OrderBy(x => x.UpdatedAt),
                    ("updatedat", "desc") => q => q.OrderByDescending(x => x.UpdatedAt),
                    _ => q => q.OrderByDescending(x => x.CreatedAt)
                };
            }

            var (vouchers, totalCount) = await _unitOfWork.Vouchers.GetPagedAsync(
                pageNumber,
                pageSize,
                v => (query.VenueOwnerId == null || v.VenueOwnerId == query.VenueOwnerId)
                    && v.IsDeleted == false
                    && (query.Status == null || v.Status == query.Status.ToString())
                    && (string.IsNullOrEmpty(keyword) || (
                        v.Code != null && v.Code.ToLower().Contains(keyword) ||
                        v.Title != null && v.Title.ToLower().Contains(keyword) ||
                        v.Description != null && v.Description.ToLower().Contains(keyword)
                    )),
                orderBy,
                v => v.Include(v => v.VenueOwner).Include(v => v.VoucherLocations).ThenInclude(vl => vl.VenueLocation)
            );

            var response = _mapper.Map<List<AdminVoucherDetailResponse>>(vouchers);

            return new PagedResult<AdminVoucherDetailResponse>
            {
                Items = response,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<AdminVoucherDetailResponse> GetAdminVoucherByIdAsync(int voucherId)
        {
            var voucher = await _unitOfWork.Vouchers.GetIncludeByIdAsync(voucherId);
            if (voucher == null)
                throw new Exception("Không tìm thấy voucher");

            return _mapper.Map<AdminVoucherDetailResponse>(voucher);
        }
    }
}
