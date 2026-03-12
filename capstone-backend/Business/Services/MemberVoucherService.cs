using AutoMapper;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.Voucher;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Services
{
    public class MemberVoucherService : IMemberVoucherService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public MemberVoucherService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<PagedResult<MemberVoucherListItemResponse>> GetMemberVouchersAsync(GetMemberVouchersRequest request)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

            var keyword = request.Keyword?.Trim().ToLower();

            // Create order
            Func<IQueryable<Voucher>, IOrderedQueryable<Voucher>> orderBy = q =>
                q.OrderByDescending(v => v.CreatedAt); // Default order

            if (!string.IsNullOrEmpty(request.SortBy))
            {
                var sortBy = request.SortBy.Trim().ToLower();
                var order = request.OrderBy?.Trim().ToLower() ?? "desc";

                orderBy = (sortBy, order) switch
                {
                    ("createdat", "asc") => q => q.OrderBy(x => x.CreatedAt),
                    ("createdat", "desc") => q => q.OrderByDescending(x => x.CreatedAt),
                    ("updatedat", "asc") => q => q.OrderBy(x => x.UpdatedAt),
                    ("updatedat", "desc") => q => q.OrderByDescending(x => x.UpdatedAt),
                    _ => q => q.OrderByDescending(x => x.CreatedAt) // Default order
                };               
            }

            var (vouchers, totalCount) = await _unitOfWork.Vouchers.GetPagedAsync(
                    pageNumber,
                    pageSize,
                    v => v.IsDeleted == false && v.Status == VoucherStatus.ACTIVE.ToString() &&
                         (string.IsNullOrEmpty(keyword) || (
                            v.Code != null && v.Code.ToLower().Contains(keyword) ||
                            v.Title != null && v.Title.ToLower().Contains(keyword) ||
                            v.Description != null && v.Description.ToLower().Contains(keyword)
                         )) &&
                         (!request.LocationId.HasValue || v.VoucherLocations.Any(x => x.VenueLocationId == request.LocationId.Value)),
                    orderBy,
                    v => v.Include(v => v.VoucherLocations).ThenInclude(vl => vl.VenueLocation)
                );

            var response = _mapper.Map<List<MemberVoucherListItemResponse>>(vouchers);

            return new PagedResult<MemberVoucherListItemResponse>
            {
                Items = response,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}
