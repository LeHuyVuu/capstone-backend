using AutoMapper;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.Voucher;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Jobs.Voucher;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Transactions;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace capstone_backend.Business.Services
{
    public class VenueVoucherService : IVenueVoucherService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IVoucherItemService _voucherItemService;

        public VenueVoucherService(IUnitOfWork unitOfWork, IMapper mapper, IVoucherItemService voucherItemService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _voucherItemService = voucherItemService;
        }

        public async Task<PagedResult<VoucherDetailResponse>> GetVenueVouchersAsync(int userId, GetVenueVouchersRequest query)
        {
            var venueOwner = await _unitOfWork.VenueOwnerProfiles.GetIncludeByUserIdAsync(userId);
            if (venueOwner == null)
                throw new Exception("Không tìm thấy chủ địa điểm");

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
                v => v.VenueOwnerId == venueOwner.Id
                    && v.IsDeleted == false
                    && (query.Status == null || v.Status == query.Status.ToString())
                    && (string.IsNullOrEmpty(keyword) || (
                        v.Code != null && v.Code.ToLower().Contains(keyword) ||
                        v.Title != null && v.Title.ToLower().Contains(keyword) ||
                        v.Description != null && v.Description.ToLower().Contains(keyword)
                    )),
                orderBy,
                v => v.Include(v => v.VoucherLocations).ThenInclude(vl => vl.VenueLocation)
            );

            var response = _mapper.Map<List<VoucherDetailResponse>>(vouchers);

            return new PagedResult<VoucherDetailResponse>
            {
                Items = response,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<VoucherDetailResponse> GetVoucherByIdAsync(int userId, int voucherId)
        {
            var venueOwner = await _unitOfWork.VenueOwnerProfiles.GetIncludeByUserIdAsync(userId);
            if (venueOwner == null)
                throw new Exception("Không tìm thấy chủ địa điểm");

            var voucher = await _unitOfWork.Vouchers.GetIncludeByIdAsync(voucherId);
            if (voucher == null || voucher.VenueOwnerId != venueOwner.Id)
                throw new Exception("Không tìm thấy voucher cho địa điểm này");

            var response = _mapper.Map<VoucherDetailResponse>(voucher);
            return response;
        }

        public async Task<VoucherSummaryResponse> GetVoucherSummaryByIdAsync(int userId, int voucherId)
        {
            var venueOwner = await _unitOfWork.VenueOwnerProfiles.GetIncludeByUserIdAsync(userId);
            if (venueOwner == null)
                throw new Exception("Không tìm thấy chủ địa điểm");

            var voucher = await _unitOfWork.Vouchers.GetIncludeByIdAsync(voucherId);
            if (voucher == null || voucher.VenueOwnerId != venueOwner.Id)
                throw new Exception("Không tìm thấy voucher cho địa điểm này");

            var voucherItems = await _unitOfWork.VoucherItems.GetAsync(vi => vi.VoucherId == voucherId && vi.IsDeleted == false);

            var response = _mapper.Map<VoucherSummaryResponse>(voucher);

            // enrich
            var acquiredCount = voucherItems.Count(vi => vi.Status == VoucherItemStatus.ACQUIRED.ToString());
            var usedCount = voucherItems.Count(vi => vi.Status == VoucherItemStatus.USED.ToString());
            var expiredCount = voucherItems.Count(vi => vi.Status == VoucherItemStatus.EXPIRED.ToString());
            var endedCount = voucherItems.Count(vi => vi.Status == VoucherItemStatus.ENDED.ToString());
            var availableCount = voucherItems.Count(vi => vi.Status == VoucherItemStatus.AVAILABLE.ToString());

            var totalQuantity = voucher.Quantity ?? 0;
            var remainingQuantity = voucher.RemainingQuantity ?? 0;

            var usageRate = totalQuantity == 0
                ? 0
                : Math.Round((decimal)usedCount * 100 / totalQuantity, 2);

            response.AcquiredCount = acquiredCount;
            response.UsedCount = usedCount;
            response.ExpiredCount = expiredCount;
            response.EndedCount = endedCount;
            response.AvailableCount = availableCount;
            response.UsageRate = usageRate;

            return response;
        }

        public async Task<PagedResult<VoucherItemResponse>> GetVoucherItemsByVoucherIdAsync(int userId, int voucherId, GetVoucherItemsRequest query)
        {
            var venueOwner = await _unitOfWork.VenueOwnerProfiles.GetIncludeByUserIdAsync(userId);
            if (venueOwner == null)
                throw new Exception("Không tìm thấy chủ địa điểm");

            var voucher = await _unitOfWork.Vouchers.GetIncludeByIdAsync(voucherId);
            if (voucher == null || voucher.VenueOwnerId != venueOwner.Id)
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

        public async Task<VoucherItemDetailResponse> GetVoucherItemByIdAsync(int userId, int voucherItemId)
        {
            var venueOwner = await _unitOfWork.VenueOwnerProfiles.GetIncludeByUserIdAsync(userId);
            if (venueOwner == null)
                throw new Exception("Không tìm thấy chủ địa điểm");

            var voucherItem = await _unitOfWork.VoucherItems.GetIncludeByIdAsync(voucherItemId);
            if (voucherItem == null)
                throw new Exception("Không tìm thấy voucher item");

            if (voucherItem.Voucher == null || voucherItem.Voucher.VenueOwnerId != venueOwner.Id)
                throw new Exception("Không tìm thấy voucher item cho địa điểm này");

            var response = _mapper.Map<VoucherItemDetailResponse>(voucherItem);
            return response;
        }

        public async Task<VoucherResponse> CreateVenueVoucherAsync(int userId, CreateVoucherRequest request)
        {
            var venueOwner = await _unitOfWork.VenueOwnerProfiles.GetIncludeByUserIdAsync(userId);
            if (venueOwner == null)
                throw new Exception("Không tìm thấy chủ địa điểm");

            if (request == null)
                throw new Exception("Dữ liệu không hợp lệ");

            if (request.VenueLocationIds == null || !request.VenueLocationIds.Any())
                throw new Exception("Voucher phải áp dụng cho ít nhất 1 địa điểm");

            foreach (var locId in request.VenueLocationIds)
            {
                var locationIds = venueOwner.VenueLocations.Select(vl => vl.Id).ToList();
                if (!locationIds.Contains(locId))
                    throw new Exception($"Địa điểm ID {locId} không thuộc quyền sở hữu của bạn");
            }

            var now = DateTime.UtcNow;

            // Check date validity
            if (request.StartDate.HasValue && request.EndDate.HasValue && request.StartDate > request.EndDate)
                throw new Exception("Ngày bắt đầu phải trước ngày kết thúc");

            if (request.StartDate.HasValue && request.StartDate.Value < now)
                throw new Exception("Ngày bắt đầu không được ở quá khứ");

            if (request.EndDate.HasValue && request.EndDate.Value < now)
                throw new Exception("Ngày kết thúc không được ở quá khứ");

            var voucher = _mapper.Map<Voucher>(request);
            voucher.VenueOwnerId = venueOwner.Id;
            voucher.RemainingQuantity = request.Quantity;
            voucher.Status = VoucherStatus.DRAFTED.ToString();

            // Generate unique voucher code
            var prefix = "VOU";
            voucher.Code = await GenerateUniqueVoucherCodeAsync(prefix);

            // Create VoucherLocation entries
            var distinctLocationIds = request.VenueLocationIds.Distinct().ToList();
            voucher.VoucherLocations = distinctLocationIds.Select(locId => new VoucherLocation
            {
                VenueLocationId = locId
            }).ToList();

            await _unitOfWork.Vouchers.AddAsync(voucher);
            await _unitOfWork.CommitTransactionAsync();

            var response = _mapper.Map<VoucherResponse>(voucher);
            return response;
        }

        public async Task<bool> DeleteVenueVoucherAsync(int userId, int voucherId)
        {
            var venueOwner = await _unitOfWork.VenueOwnerProfiles.GetIncludeByUserIdAsync(userId);
            if (venueOwner == null)
                throw new Exception("Không tìm thấy chủ địa điểm");

            var voucher = await _unitOfWork.Vouchers.GetIncludeByIdAsync(voucherId);
            if (voucher == null)
                throw new Exception("Không tìm thấy voucher");

            if (voucher.VenueOwnerId != venueOwner.Id)
                throw new Exception("Bạn không có quyền xóa voucher này");

            if ((voucher.Status != VoucherStatus.DRAFTED.ToString()) &&
                (voucher.Status != VoucherStatus.REJECTED.ToString()))
                throw new Exception("Chỉ có thể xóa voucher ở trạng thái DRAFTED hoặc REJECTED");

            voucher.IsDeleted = true;
            voucher.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Vouchers.Update(voucher);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<VoucherResponse> RevokeSubmittedVoucherAsync(int userId, int voucherId)
        {
            var venueOwner = await _unitOfWork.VenueOwnerProfiles.GetIncludeByUserIdAsync(userId);
            if (venueOwner == null)
                throw new Exception("Không tìm thấy chủ địa điểm");

            var voucher = await _unitOfWork.Vouchers.GetIncludeByIdAsync(voucherId);
            if (voucher == null)
                throw new Exception("Không tìm thấy voucher");

            if (voucher.VenueOwnerId != venueOwner.Id)
                throw new Exception("Bạn không có quyền thu hồi yêu cầu duyệt voucher này");

            if (voucher.Status != VoucherStatus.PENDING.ToString())
                throw new Exception("Chỉ có thể thu hồi yêu cầu duyệt voucher ở trạng thái PENDING");

            voucher.Status = VoucherStatus.DRAFTED.ToString();
            voucher.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Vouchers.Update(voucher);
            await _unitOfWork.SaveChangesAsync();

            var response = _mapper.Map<VoucherResponse>(voucher);
            return response;
        }

        public async Task<VoucherResponse> SubmitVoucherAsync(int userId, int voucherId)
        {
            var venueOwner = await _unitOfWork.VenueOwnerProfiles.GetIncludeByUserIdAsync(userId);
            if (venueOwner == null)
                throw new Exception("Không tìm thấy chủ địa điểm");

            var voucher = await _unitOfWork.Vouchers.GetIncludeByIdAsync(voucherId);
            if (voucher == null)
                throw new Exception("Không tìm thấy voucher");

            if (voucher.VenueOwnerId != venueOwner.Id)
                throw new Exception("Bạn không có quyền gửi voucher này để xét duyệt");

            if (voucher.Status != VoucherStatus.DRAFTED.ToString())
                throw new Exception("Chỉ có thể voucher để xét duyệt ở trạng thái DRAFTED");

            // Check if have 1 location
            if (voucher.VoucherLocations == null || !voucher.VoucherLocations.Any())
                throw new Exception("Voucher phải áp dụng cho ít nhất 1 địa điểm");

            // Check quantity
            if (voucher.Quantity <= 0)
                throw new Exception("Số lượng phải lớn hơn 0");

            // Check date validity
            var now = DateTime.UtcNow;
            if (voucher.StartDate.HasValue && voucher.EndDate.HasValue && voucher.StartDate > voucher.EndDate)
                throw new Exception("Ngày bắt đầu phải trước ngày kết thúc");

            if (voucher.StartDate.HasValue && voucher.StartDate.Value < now)
                throw new Exception("Ngày bắt đầu không được ở quá khứ");

            if (voucher.EndDate.HasValue && voucher.EndDate.Value < now)
                throw new Exception("Ngày kết thúc không được ở quá khứ");

            // Check discount validity
            if (voucher.DiscountType == VoucherDiscountType.FIXED_AMOUNT.ToString())
            {
                if (!voucher.DiscountAmount.HasValue || voucher.DiscountAmount.Value <= 0)
                    throw new Exception("Số tiền giảm phải lớn hơn 0");
            }
            else if (voucher.DiscountType == VoucherDiscountType.PERCENTAGE.ToString())
            {
                if (!voucher.DiscountPercent.HasValue || voucher.DiscountPercent.Value < 1 || voucher.DiscountPercent.Value > 100)
                    throw new Exception("Phần trăm giảm phải từ 1 đến 100");
            }
            else
            {
                throw new Exception("Loại giảm giá không hợp lệ");
            }

            voucher.Status = VoucherStatus.PENDING.ToString();
            voucher.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Vouchers.Update(voucher);
            await _unitOfWork.SaveChangesAsync();

            var response = _mapper.Map<VoucherResponse>(voucher);
            return response;
        }

        public async Task<VoucherResponse> UpdateVenueVoucherAsync(int userId, int voucherId, UpdateVoucherRequest request)
        {
            var venueOwner = await _unitOfWork.VenueOwnerProfiles.GetIncludeByUserIdAsync(userId);
            if (venueOwner == null)
                throw new Exception("Không tìm thấy chủ địa điểm");

            if (request == null)
                throw new Exception("Dữ liệu không hợp lệ");

            var voucher = await _unitOfWork.Vouchers.GetIncludeByIdAsync(voucherId);
            if (voucher == null)
                throw new Exception("Không tìm thấy voucher");

            if ((voucher.Status != VoucherStatus.DRAFTED.ToString()) &&
                (voucher.Status != VoucherStatus.REJECTED.ToString()))
                throw new Exception("Chỉ có thể cập nhật voucher ở trạng thái DRAFTED hoặc REJECTED");

            if (voucher.VenueOwnerId != venueOwner.Id)
                throw new Exception("Bạn không có quyền cập nhật voucher này");

            var now = DateTime.UtcNow;

            // Resolve date after update
            var newStartDate = request.StartDate ?? voucher.StartDate;
            var newEndDate = request.EndDate ?? voucher.EndDate;

            if (newStartDate.HasValue && newEndDate.HasValue && newStartDate > newEndDate)
                throw new Exception("Ngày bắt đầu phải trước ngày kết thúc");

            if (request.StartDate.HasValue && request.StartDate.Value < now)
                throw new Exception("Ngày bắt đầu không được ở quá khứ");

            if (request.EndDate.HasValue && request.EndDate.Value < now)
                throw new Exception("Ngày kết thúc không được ở quá khứ");

            if (request.VenueLocationIds != null)
            {
                if (!request.VenueLocationIds.Any())
                    throw new Exception("Voucher phải áp dụng cho ít nhất 1 địa điểm");

                var ownerLocationIds = venueOwner.VenueLocations.Select(vl => vl.Id).ToHashSet();

                foreach (var locId in request.VenueLocationIds.Distinct())
                {
                    if (!ownerLocationIds.Contains(locId))
                        throw new Exception($"Địa điểm ID {locId} không thuộc quyền sở hữu của bạn");
                }
            }

            // Map
            _mapper.Map(request, voucher);

            if (request.VenueLocationIds != null)
            {
                var distinctLocationIds = request.VenueLocationIds.Distinct().ToList();
                voucher.VoucherLocations = distinctLocationIds.Select(locId => new VoucherLocation
                {
                    VenueLocationId = locId
                }).ToList();
            }

            // Change status back to DRAFTED if it was REJECTED
            if (voucher.Status == VoucherStatus.REJECTED.ToString())
                voucher.Status = VoucherStatus.DRAFTED.ToString();

            // Update remaining quantity if quantity changed
            if (voucher.Quantity != voucher.RemainingQuantity)
                voucher.RemainingQuantity = voucher.Quantity;

            _unitOfWork.Vouchers.Update(voucher);
            await _unitOfWork.SaveChangesAsync();

            var response = _mapper.Map<VoucherResponse>(voucher);
            return response;
        }

        private async Task<string> GenerateUniqueVoucherCodeAsync(string prefix)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new Random();
            string code = string.Empty;
            bool isDuplicate = true;

            while (isDuplicate)
            {
                // Gen 6 random chars
                var randomString = new string(Enumerable.Repeat(chars, 6)
                    .Select(s => s[random.Next(s.Length)]).ToArray());

                // Format: VOU26-X7KL9M
                code = $"{prefix}{DateTime.Now:yy}-{randomString}";

                // Check if code already exists in DB
                isDuplicate = await _unitOfWork.Vouchers.IsDuplicateCodeAsync(code);
            }

            return code;
        }

        public async Task<int> ActivateVoucherAsync(int userId, int voucherId)
        {
            var venueOwner = await _unitOfWork.VenueOwnerProfiles.GetIncludeByUserIdAsync(userId);
            if (venueOwner == null)
                throw new Exception("Không tìm thấy chủ địa điểm");

            var voucher = await _unitOfWork.Vouchers.GetIncludeByIdAsync(voucherId);
            if (voucher == null)
                throw new Exception("Không tìm thấy voucher");

            if (voucher.VenueOwnerId != venueOwner.Id)
                throw new Exception("Bạn không có quyền kích hoạt voucher này");

            if (voucher.Status != VoucherStatus.APPROVED.ToString())
                throw new Exception("Chỉ có thể kích hoạt voucher ở trạng thái APPROVED");

            var now = DateTime.UtcNow;

            if (voucher.EndDate.HasValue && voucher.EndDate.Value <= now)
                throw new Exception("Không thể kích hoạt voucher đã hết hạn");

            if (!voucher.Quantity.HasValue || voucher.Quantity.Value <= 0)
                throw new Exception("Số lượng voucher không hợp lệ");

            // Remove start job if exist
            var startJob = await _unitOfWork.VoucherJobs.GetByVoucherIdAndTypeAsync(voucher.Id, VoucherJobType.ACTIVATE_VOUCHER.ToString());
            if (startJob != null)
            {
                BackgroundJob.Delete(startJob.JobId);
                _unitOfWork.VoucherJobs.Delete(startJob);
            }

            voucher.Status = VoucherStatus.ACTIVE.ToString();
            voucher.UpdatedAt = now;

            _unitOfWork.Vouchers.Update(voucher);          

            // call to generate voucher item code
            await _voucherItemService.GenerateVoucherItemsAsync(voucher.Id, voucher.Quantity.Value);

            await _unitOfWork.SaveChangesAsync();

            return voucher.Id;
        }

        public async Task<int> EndVoucherAsync(int userId, int voucherId)
        {
            var venueOwner = await _unitOfWork.VenueOwnerProfiles.GetIncludeByUserIdAsync(userId);
            if (venueOwner == null)
                throw new Exception("Không tìm thấy chủ địa điểm");

            var voucher = await _unitOfWork.Vouchers.GetIncludeByIdAsync(voucherId);
            if (voucher == null)
                throw new Exception("Không tìm thấy voucher");

            if (voucher.VenueOwnerId != venueOwner.Id)
                throw new Exception("Bạn không có quyền kết thúc voucher này");

            if (voucher.Status != VoucherStatus.ACTIVE.ToString() && 
                voucher.Status != VoucherStatus.APPROVED.ToString())
                throw new Exception("Chỉ có thể kết thúc voucher ở trạng thái ACTIVE hoặc APPROVED");

            var now = DateTime.UtcNow;

            // Remove start job for approved voucher if exist
            if (voucher.Status == VoucherStatus.APPROVED.ToString())
            {
                var startJob = await _unitOfWork.VoucherJobs.GetByVoucherIdAndTypeAsync(voucher.Id, VoucherJobType.ACTIVATE_VOUCHER.ToString());
                if (startJob != null)
                {
                    BackgroundJob.Delete(startJob.JobId);
                    _unitOfWork.VoucherJobs.Delete(startJob);
                }
            }

            // Remove end job if exist
            var endJob = await _unitOfWork.VoucherJobs.GetByVoucherIdAndTypeAsync(voucher.Id, VoucherJobType.END_VOUCHER.ToString());
            if (endJob != null)
            {
                BackgroundJob.Delete(endJob.JobId);
                _unitOfWork.VoucherJobs.Delete(endJob);
            }

            // Update all remaining voucher items to end
            await _unitOfWork.VoucherItems.ExecuteUpdateUnassignedVoucherItemsAsync(voucher.Id);

            voucher.Status = VoucherStatus.ENDED.ToString();
            voucher.UpdatedAt = now;

            _unitOfWork.Vouchers.Update(voucher);
            await _unitOfWork.SaveChangesAsync();

            return voucher.Id;
        }
    }
}
