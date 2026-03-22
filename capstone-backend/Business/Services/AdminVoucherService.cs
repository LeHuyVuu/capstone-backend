using AutoMapper;
using capstone_backend.Business.Configurations;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.Voucher;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Jobs.Voucher;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace capstone_backend.Business.Services
{
    public class AdminVoucherService : IAdminVoucherService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IVoucherItemService _voucherItemService;
        private readonly decimal _vndPerPoint;

        public AdminVoucherService(IUnitOfWork unitOfWork, IMapper mapper, IVoucherItemService voucherItemService, IOptions<PointSettings> pointSettings)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _voucherItemService = voucherItemService;
            _vndPerPoint = pointSettings.Value.VndPerPoint;
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
                    && v.Status != VoucherStatus.DRAFTED.ToString()
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

        public async Task<PagedResult<AdminVoucherDetailResponse>> GetPendingVouchersAsync(GetPendingVouchersRequest query)
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
                    && v.Status == VoucherStatus.PENDING.ToString()
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

        public async Task<int> ApproveVoucherAsync(int voucherId)
        {
            var voucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId);
            if (voucher == null)
                throw new Exception("Không tìm thấy voucher");

            if (voucher.Status != VoucherStatus.PENDING.ToString())
                throw new Exception("Voucher không ở trạng thái chờ duyệt (PENDING)");

            var now = DateTime.UtcNow;

            if (voucher.EndDate.HasValue && voucher.EndDate.Value <= now)
                throw new Exception("Không thể duyệt voucher đã hết hạn");

            if (voucher.StartDate.HasValue && voucher.StartDate.Value > now)
            {
                voucher.Status = VoucherStatus.APPROVED.ToString();
            }
            else
            {
                voucher.Status = VoucherStatus.ACTIVE.ToString();

                // call to generate voucher item code
                await _voucherItemService.GenerateVoucherItemsAsync(voucher.Id, voucher.Quantity ?? 0);
            }

            voucher.UpdatedAt = now;
            voucher.RejectReason = null; // clear reject reason if any

            // Update point price
            voucher.PointPrice = (int)Math.Floor(voucher.VoucherPrice / _vndPerPoint);

            _unitOfWork.Vouchers.Update(voucher);
            await _unitOfWork.SaveChangesAsync();

            var voucherJobs = new List<VoucherJob>();

            // Add job for auto publish voucher at StartDate
            if (voucher.StartDate.HasValue && voucher.StartDate.Value > now)
            {
                // Auto start
                var activeJob = BackgroundJob.Schedule<IVoucherWorker>(
                    job => job.ActivateVoucherAsync(voucher.Id),
                    voucher.StartDate.Value - now
                );
                voucherJobs.Add(new VoucherJob
                {
                    VoucherId = voucher.Id,
                    JobId = activeJob,
                    JobType = VoucherJobType.ACTIVATE_VOUCHER.ToString()
                });
            }

            // Auto expire
            if (voucher.EndDate.HasValue && voucher.EndDate.Value > now)
            {
                var endedJob = BackgroundJob.Schedule<IVoucherWorker>(
                    job => job.EndVoucherAsync(voucher.Id),
                    voucher.EndDate.Value - now
                );
                voucherJobs.Add(new VoucherJob
                {
                    VoucherId = voucher.Id,
                    JobId = endedJob,
                    JobType = VoucherJobType.END_VOUCHER.ToString()
                });
            }

            // Save job id to database
            if (voucherJobs.Any())
            {
                await _unitOfWork.VoucherJobs.AddRangeAsync(voucherJobs);
                await _unitOfWork.SaveChangesAsync();
            }

            return voucher.Id;
        }

        public async Task<int> RejectVoucherAsync(int voucherId, RejectReasonRequest request)
        {
            var voucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId);
            if (voucher == null)
                throw new Exception("Không tìm thấy voucher");

            if (voucher.Status != VoucherStatus.PENDING.ToString())
                throw new Exception("Voucher không ở trạng thái chờ duyệt (PENDING)");

            if (request == null)
                throw new Exception("Dữ liệu không hợp lệ");

            if (string.IsNullOrWhiteSpace(request.RejectReason))
                throw new Exception("Vui lòng nhập lý do từ chối");

            voucher.RejectReason = request.RejectReason;
            voucher.Status = VoucherStatus.REJECTED.ToString();
            voucher.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Vouchers.Update(voucher);
            await _unitOfWork.SaveChangesAsync();

            return voucher.Id;
        }

        public async Task<PagedResult<VoucherItemResponse>> GetVoucherItemAsync(int voucherId, GetVoucherItemsRequest query)
        {
            var voucher = await _unitOfWork.Vouchers.GetIncludeByIdAsync(voucherId);
            if (voucher == null)
                throw new Exception("Không tìm thấy voucher cho địa điểm này");

            int pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
            int pageSize = query.PageSize < 1 ? 10 : query.PageSize;

            var keyword = query.Code?.Trim().ToLower();

            var (voucherItems, totalCount) = await _unitOfWork.VoucherItems.GetPagedAsync(
                pageNumber,
                pageSize,
                vi => vi.VoucherId == voucherId
                    && (query.Status == null || vi.Status == query.Status.ToString())
                    && vi.IsDeleted == false
                    && (string.IsNullOrEmpty(keyword) || (
                        vi.ItemCode != null && vi.ItemCode.ToLower().Contains(keyword)
                    )),
                q => q.OrderByDescending(vi => vi.CreatedAt)
            );

            var response = _mapper.Map<List<VoucherItemResponse>>(voucherItems);
            return new PagedResult<VoucherItemResponse>
            {
                Items = response,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<VoucherItemDetailResponse> GetVoucherItemByIdAsync(int voucherItemId)
        {
            var voucherItem = await _unitOfWork.VoucherItems.GetIncludeByIdAsync(voucherItemId);
            if (voucherItem == null)
                throw new Exception("Không tìm thấy voucher item");

            var response = _mapper.Map<VoucherItemDetailResponse>(voucherItem);
            return response;
        }
    }
}