using AutoMapper;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.Voucher;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Jobs.Voucher;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using Hangfire;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace capstone_backend.Business.Services
{
    public class VenueVoucherService : IVenueVoucherService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IVoucherItemService _voucherItemService;
        private readonly ISystemConfigService _systemConfigService;

        public VenueVoucherService(IUnitOfWork unitOfWork, IMapper mapper, IVoucherItemService voucherItemService, ISystemConfigService systemConfigService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _voucherItemService = voucherItemService;
            _systemConfigService = systemConfigService;
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

            response.TotalQuantity = totalQuantity;
            response.AcquiredCount = acquiredCount;
            response.UsedCount = usedCount;
            response.ExpiredCount = expiredCount;
            response.EndedCount = endedCount;
            response.AvailableCount = availableCount;
            response.UsageRate = usageRate;

            response.PointPrice = voucher.PointPrice;
            response.TotalPointsExchanged = (totalQuantity - remainingQuantity) * voucher.PointPrice;

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

            //if (request.VenueLocationIds == null || !request.VenueLocationIds.Any())
            //    throw new Exception("Voucher phải áp dụng cho ít nhất 1 địa điểm");

            var now = DateTime.UtcNow;

            // Check date validity
            //if (request.StartDate.HasValue && request.EndDate.HasValue && request.StartDate > request.EndDate)
            //    throw new Exception("Ngày bắt đầu phải trước ngày kết thúc");

            //if (request.StartDate.HasValue && request.StartDate.Value < now)
            //    throw new Exception("Ngày bắt đầu không được ở quá khứ");

            //if (request.EndDate.HasValue && request.EndDate.Value < now)
            //    throw new Exception("Ngày kết thúc không được ở quá khứ");

            var voucher = _mapper.Map<Voucher>(request);
            voucher.VenueOwnerId = venueOwner.Id;
            voucher.RemainingQuantity = request.Quantity;
            voucher.Status = VoucherStatus.DRAFTED.ToString();

            const string validImagePrefix = "https://couplemood-store.s3.ap-southeast-2.amazonaws.com/";

            voucher.ImageUrl =
                !string.IsNullOrWhiteSpace(voucher.ImageUrl) &&
                voucher.ImageUrl.StartsWith(validImagePrefix, StringComparison.OrdinalIgnoreCase)
                    ? voucher.ImageUrl
                    : "https://couplemood-store.s3.ap-southeast-2.amazonaws.com/system/voucher_thumbnail.png";

            // Generate unique voucher code
            var prefix = "VOU";
            voucher.Code = await GenerateUniqueVoucherCodeAsync(prefix);

            // Create VoucherLocation entries
            // Check valid locations
            //var distinctLocationIds = request.VenueLocationIds.Distinct().ToList();

            //var invalidLocations = distinctLocationIds
            //    .Select(id => new
            //    {
            //        Id = id,
            //        Location = venueOwner.VenueLocations.FirstOrDefault(loc =>
            //            loc.Id == id &&
            //            loc.Status == VenueLocationStatus.ACTIVE.ToString() &&
            //            loc.IsDeleted == false)
            //    })
            //    .Where(x => x.Location == null)
            //    .ToList();

            //if (invalidLocations.Any())
            //{
            //    var errorMessages = invalidLocations.Select(x => {
            //        var existingLoc = venueOwner.VenueLocations.FirstOrDefault(l => l.Id == x.Id);
            //        return existingLoc != null ? $"'{existingLoc.Name}' (ID: {x.Id})" : $"(ID: {x.Id} - Không tồn tại)";
            //    });

            //    throw new Exception($"Các địa điểm sau không hợp lệ hoặc ngưng hoạt động: {string.Join(", ", errorMessages)}");
            //}

            //voucher.VoucherLocations = distinctLocationIds.Select(locId => new VoucherLocation
            //{
            //    VenueLocationId = locId
            //}).ToList();

            await _unitOfWork.Vouchers.AddAsync(voucher);
            await _unitOfWork.SaveChangesAsync();
            //await _unitOfWork.CommitTransactionAsync();

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

            ValidateVoucherBeforeSubmit(voucher);

            // Check if have 1 location
            if (voucher.VoucherLocations == null || !voucher.VoucherLocations.Any())
                throw new Exception("Voucher phải áp dụng cho ít nhất 1 địa điểm");

            var inactiveLocations = voucher.VoucherLocations
                .Where(vl => vl.VenueLocation.Status != VenueLocationStatus.ACTIVE.ToString() || vl.VenueLocation.IsDeleted == true)
                .Select(vl => vl.VenueLocation.Name)
                .ToList();

            if (inactiveLocations.Any())
                throw new Exception($"Không thể gửi duyệt. Các địa điểm sau đang ngưng hoạt động: {string.Join(", ", inactiveLocations)}");

            //// Check quantity
            //if (voucher.Quantity <= 0)
            //    throw new Exception("Số lượng phải lớn hơn 0");

            //// Check date validity
            //var now = DateTime.UtcNow;
            //if (voucher.StartDate.HasValue && voucher.EndDate.HasValue && voucher.StartDate > voucher.EndDate)
            //    throw new Exception("Ngày bắt đầu phải trước ngày kết thúc");

            //if (voucher.StartDate.HasValue && voucher.StartDate.Value < now)
            //    throw new Exception("Ngày bắt đầu không được ở quá khứ");

            //if (voucher.EndDate.HasValue && voucher.EndDate.Value < now)
            //    throw new Exception("Ngày kết thúc không được ở quá khứ");

            //// Check discount validity
            //if (voucher.DiscountType == VoucherDiscountType.FIXED_AMOUNT.ToString())
            //{
            //    if (!voucher.DiscountAmount.HasValue || voucher.DiscountAmount.Value <= 0)
            //        throw new Exception("Số tiền giảm phải lớn hơn 0");
            //}
            //else if (voucher.DiscountType == VoucherDiscountType.PERCENTAGE.ToString())
            //{
            //    if (!voucher.DiscountPercent.HasValue || voucher.DiscountPercent.Value < 1 || voucher.DiscountPercent.Value > 100)
            //        throw new Exception("Phần trăm giảm phải từ 1 đến 100");
            //}
            //else
            //{
            //    throw new Exception("Loại giảm giá không hợp lệ");
            //}

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
            }

            // Map
            _mapper.Map(request, voucher);

            if (request.VenueLocationIds != null)
            {
                var distinctLocationIds = request.VenueLocationIds.Distinct().ToList();
                var invalidInfo = distinctLocationIds
                    .Select(id => new
                    {
                        Id = id,
                        Location = venueOwner.VenueLocations.FirstOrDefault(loc =>
                            loc.Id == id &&
                            loc.Status == VenueLocationStatus.ACTIVE.ToString() &&
                            loc.IsDeleted == false)
                    })
                    .Where(x => x.Location == null)
                    .ToList();

                if (invalidInfo.Any())
                {
                    var errorMessages = invalidInfo.Select(x => {
                        var exist = venueOwner.VenueLocations.FirstOrDefault(l => l.Id == x.Id);
                        return exist != null ? $"'{exist.Name}'" : $"ID:{x.Id}";
                    });

                    throw new Exception($"Các địa điểm sau không hợp lệ hoặc ngưng hoạt động: {string.Join(", ", errorMessages)}");
                }

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

            const string validImagePrefix = "https://couplemood-store.s3.ap-southeast-2.amazonaws.com/";

            voucher.ImageUrl =
                !string.IsNullOrWhiteSpace(voucher.ImageUrl) &&
                voucher.ImageUrl.StartsWith(validImagePrefix, StringComparison.OrdinalIgnoreCase)
                    ? voucher.ImageUrl
                    : "https://couplemood-store.s3.ap-southeast-2.amazonaws.com/system/voucher_thumbnail.png";

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

            var inactiveLocations = voucher.VoucherLocations
                .Where(vl => vl.VenueLocation.Status != VenueLocationStatus.ACTIVE.ToString() || vl.VenueLocation.IsDeleted == true)
                .Select(vl => vl.VenueLocation.Name)
                .ToList();

            var inactiveCount = voucher.VoucherLocations
                .Count(vl => vl.VenueLocation.Status != VenueLocationStatus.ACTIVE.ToString() || vl.VenueLocation.IsDeleted == true);

            if (inactiveCount > 0 && inactiveCount == voucher.VoucherLocations.Count)
                throw new Exception("Không thể kích hoạt voucher vì TẤT CẢ địa điểm áp dụng đều đã ngưng hoạt động.");

            if (inactiveLocations.Any())
                throw new Exception($"Không thể kích hoạt voucher. Các địa điểm sau đang ngưng hoạt động: {string.Join(", ", inactiveLocations)}");

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
            voucher.StartDate = now;
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

        public async Task<VoucherItemValidationAndRedemptionResponse> ValidateVoucherCodeAsync(int userId, ValidateAndRedeemVoucherItemRequest request)
        {
            var validationMessage = string.Empty;

            //var venueOwner = await _unitOfWork.VenueOwnerProfiles.GetIncludeByUserIdAsync(userId);
            //if (venueOwner == null)
            //    throw new Exception("Không tìm thấy chủ địa điểm");

            var voucherItem = await _unitOfWork.VoucherItems.GetByItemCodeWithDetailsAsync(request.ItemCode);
            if (voucherItem == null)
                throw new Exception("Mã voucher không hợp lệ");

            if (voucherItem.Voucher == null)
                throw new Exception("Mã voucher không hợp lệ");

            //if (voucherItem.Voucher.VenueOwnerId != venueOwner.Id)
            //    throw new Exception("Bạn không có quyền xác thực voucher này");

            var response = _mapper.Map<VoucherItemValidationAndRedemptionResponse>(voucherItem);

            var now = DateTime.UtcNow;

            if (voucherItem.VoucherItemMemberId == null)
            {
                response.IsValid = false;
                response.ValidationMessage = "Mã voucher chưa có người sở hữu";
                return response;
            }

            if (voucherItem.Status == VoucherItemStatus.USED.ToString())
            {
                response.IsValid = false;
                response.ValidationMessage = "Mã voucher đã được sử dụng";
                return response;
            }

            if ((voucherItem.ExpiredAt.HasValue && voucherItem.ExpiredAt.Value <= now)
                || voucherItem.Status == VoucherItemStatus.EXPIRED.ToString())
            {
                response.IsValid = false;
                response.ValidationMessage = "Mã voucher đã hết hạn";
                return response;
            }

            if (voucherItem.Status != VoucherItemStatus.ACQUIRED.ToString())
            {
                response.IsValid = false;
                response.ValidationMessage = "Mã voucher không ở trạng thái có thể sử dụng";
                return response;
            }
            
            // Check location
            var voucherLocations = voucherItem.Voucher.VoucherLocations;
            if (voucherLocations != null && voucherLocations.Any())
            {
                var isValidLocation = voucherLocations.Any(vl => vl.VenueLocationId == request.VenueLocationId && vl.VenueLocation.Status == VenueLocationStatus.ACTIVE.ToString());

                if (!isValidLocation)
                {
                    response.IsValid = false;
                    response.ValidationMessage = "Mã voucher không áp dụng cho địa điểm này hoặc địa điểm không hoạt động";
                    return response;
                }
            }

            response.IsValid = true;
            response.ValidationMessage = "Mã voucher hợp lệ";
            return response;
        }

        public async Task<VoucherItemValidationAndRedemptionResponse?> RedeemVoucherCodeAsync(int userId, ValidateAndRedeemVoucherItemRequest request)
        {
            //var venueOwner = await _unitOfWork.VenueOwnerProfiles.GetIncludeByUserIdAsync(userId);
            //if (venueOwner == null)
            //    throw new Exception("Không tìm thấy chủ địa điểm");

            var voucherItem = await _unitOfWork.VoucherItems.GetByItemCodeWithDetailsAsync(request.ItemCode);
            if (voucherItem == null)
                throw new Exception("Mã voucher không hợp lệ");

            if (voucherItem.Voucher == null)
                throw new Exception("Mã voucher không hợp lệ");

            //if (voucherItem.Voucher.VenueOwnerId != venueOwner.Id)
            //    throw new Exception("Bạn không có quyền xác thực voucher này");

            var response = _mapper.Map<VoucherItemValidationAndRedemptionResponse>(voucherItem);
            var now = DateTime.UtcNow;

            if (voucherItem.Voucher.Status != VoucherStatus.ACTIVE.ToString())
            {
                response.IsValid = false;
                response.ValidationMessage = "Voucher hiện không khả dụng";
                return response;
            }

            if (voucherItem.VoucherItemMemberId == null)
            {
                response.IsValid = false;
                response.ValidationMessage = "Mã voucher chưa có người sở hữu";
                return response;
            }

            if (voucherItem.Status == VoucherItemStatus.USED.ToString())
            {
                response.IsValid = false;
                response.ValidationMessage = "Mã voucher đã được sử dụng";
                return response;
            }

            if ((voucherItem.ExpiredAt.HasValue && voucherItem.ExpiredAt.Value <= now)
                || voucherItem.Status == VoucherItemStatus.EXPIRED.ToString())
            {
                response.IsValid = false;
                response.ValidationMessage = "Mã voucher đã hết hạn";
                return response;
            }

            if (voucherItem.Status != VoucherItemStatus.ACQUIRED.ToString())
            {
                response.IsValid = false;
                response.ValidationMessage = "Mã voucher không ở trạng thái có thể sử dụng";
                return response;
            }

            // Check location
            var voucherLocations = voucherItem.Voucher.VoucherLocations;
            if (voucherLocations != null && voucherLocations.Any())
            {
                var isValidLocation = voucherLocations.Any(vl => vl.VenueLocationId == request.VenueLocationId && vl.VenueLocation.Status == VenueLocationStatus.ACTIVE.ToString());

                if (!isValidLocation)
                {
                    response.IsValid = false;
                    response.ValidationMessage = "Mã voucher không áp dụng cho địa điểm này hoặc địa điểm không hoạt động";
                    return response;
                }
            }

            // Remove job for auto expire if exist
            var expireJob = await _unitOfWork.VoucherItemJobs.GetByVoucherItemIdAndTypeAsync(voucherItem.Id, VoucherItemJobType.EXPIRE_VOUCHER_ITEM.ToString());
            if (expireJob != null)
            {
                BackgroundJob.Delete(expireJob.JobId);
                _unitOfWork.VoucherItemJobs.Delete(expireJob);
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Update status to USED
                voucherItem.Status = VoucherItemStatus.USED.ToString();
                voucherItem.UsedAt = now;
                voucherItem.UpdatedAt = now;

                _unitOfWork.VoucherItems.Update(voucherItem);
                await _unitOfWork.SaveChangesAsync();

                // Create pending settlement
                var commissionPercent = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.VENUE_COMMISSION_PERCENT.ToString());

                var grossAmount = voucherItem.Voucher?.VoucherPrice ?? 0;
                var commissionRate = commissionPercent / 100m;
                var commissionAmount = grossAmount * commissionRate;
                var netAmount = grossAmount - commissionAmount;

                await _unitOfWork.VenueSettlements.AddAsync(new VenueSettlement
                {
                    VoucherItemId = voucherItem.Id,
                    VoucherItemMemberId = voucherItem.VoucherItemMemberId,
                    VenueOwnerId = voucherItem.Voucher.VenueOwnerId.Value,

                    GrossAmount = grossAmount,
                    CommissionAmount = commissionAmount,
                    NetAmount = netAmount,

                    Status = VenueSettlementStatus.PENDING.ToString(),
                    AvailableAt = DateTime.UtcNow.AddDays(3),
                    Note = $"Settlement for voucher item {voucherItem.ItemCode}"
                });

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return null;
            }

            var redeemedResponse = _mapper.Map<VoucherItemValidationAndRedemptionResponse>(voucherItem);
            redeemedResponse.IsValid = true;
            redeemedResponse.ValidationMessage = "Mã voucher đã được sử dụng thành công";
            return redeemedResponse;
        }

        public async Task<PagedResult<VenueVoucherActivityResponse>> GetVoucherRedemptionsAsync(int userId, int voucherId, GetVoucherActivityRequest query)
        {
            var venueOwner = await _unitOfWork.VenueOwnerProfiles.GetIncludeByUserIdAsync(userId);
            if (venueOwner == null)
                throw new Exception("Không tìm thấy chủ địa điểm");

            var voucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId);
            if (voucher == null || voucher.IsDeleted == true)
                throw new Exception("Không tìm thấy voucher");

            if (voucher.VenueOwnerId != venueOwner.Id)
                throw new Exception("Bạn không có quyền xem lịch sử sử dụng voucher này");

            if (query.FromDate.HasValue && query.ToDate.HasValue && query.FromDate.Value > query.ToDate.Value)
                throw new Exception("FromDate phải nhỏ hơn hoặc bằng ToDate");

            var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
            var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

            var keyword = query.Keyword?.Trim().ToLower();

            Func<IQueryable<VoucherItem>, IOrderedQueryable<VoucherItem>> orderBy = q =>
                q.OrderByDescending(vi => vi.UsedAt);

            if (!string.IsNullOrWhiteSpace(query.OrderBy))
            {
                var order = query.OrderBy?.Trim().ToLower() ?? "desc";

                orderBy = (order) switch
                {
                    ("asc") => q => q.OrderBy(vi => vi.UsedAt),
                    ("desc") => q => q.OrderByDescending(vi => vi.UsedAt),
                    _ => q => q.OrderByDescending(vi => vi.UsedAt)
                };
            }

            var (voucherItems, totalCount) = await _unitOfWork.VoucherItems.GetPagedAsync(
                pageNumber,
                pageSize,
                vi => vi.VoucherId == voucherId
                    && vi.VoucherItemMemberId != null
                    && vi.UsedAt != null
                    && vi.Status == VoucherItemStatus.USED.ToString()
                    && vi.IsDeleted == false
                    && (!query.FromDate.HasValue || vi.UsedAt >= query.FromDate.Value)
                    && (!query.ToDate.HasValue || vi.UsedAt <= query.ToDate.Value)
                    && (
                    string.IsNullOrEmpty(keyword) ||
                    (
                        (vi.ItemCode != null && vi.ItemCode.ToLower().Contains(keyword)) ||
                        (vi.VoucherItemMember != null &&
                             vi.VoucherItemMember.Member != null &&
                             vi.VoucherItemMember.Member.FullName != null &&
                             vi.VoucherItemMember.Member.FullName.ToLower().Contains(keyword)) ||
                        (vi.VoucherItemMember != null &&
                             vi.VoucherItemMember.Member != null &&
                             vi.VoucherItemMember.Member.User != null &&
                             vi.VoucherItemMember.Member.User.Email != null &&
                             vi.VoucherItemMember.Member.User.Email.ToLower().Contains(keyword)) ||
                         (vi.VoucherItemMember != null &&
                             vi.VoucherItemMember.Member != null &&
                             vi.VoucherItemMember.Member.User != null &&
                             vi.VoucherItemMember.Member.User.PhoneNumber != null &&
                             vi.VoucherItemMember.Member.User.PhoneNumber.ToLower().Contains(keyword)
                         )
                    )
                ),
                orderBy,
                q => q.Include(vi => vi.VoucherItemMember).ThenInclude(vim => vim.Member).ThenInclude(m => m.User)
            );

            var response = voucherItems.Select(vi =>
            {
                return new VenueVoucherActivityResponse
                {
                    VoucherId = vi.VoucherId,
                    VoucherItemId = vi.Id,
                    VoucherItemMemberId = vi.VoucherItemMemberId,

                    ItemCode = vi.ItemCode,
                    Status = vi.Status,

                    MemberId = vi.VoucherItemMember?.MemberId,
                    MemberName = vi.VoucherItemMember?.Member?.FullName,
                    MemberEmail = vi.VoucherItemMember?.Member?.User?.Email,
                    MemberPhone = vi.VoucherItemMember?.Member?.User?.PhoneNumber,

                    Quantity = vi.VoucherItemMember?.Quantity ?? 0,
                    TotalPointsUsed = vi.VoucherItemMember?.TotalPointsUsed ?? 0,
                    Note = vi.VoucherItemMember?.Note,

                    AcquiredAt = vi.AcquiredAt,
                    UsedAt = vi.UsedAt,
                    ExpiredAt = vi.ExpiredAt
                };
            }).ToList();

            return new PagedResult<VenueVoucherActivityResponse>
            {
                Items = response,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<VenueVoucherActivityResponse>> GetVoucherExchangesAsync(int userId, int voucherId, GetVoucherActivityRequest query)
        {
            var venueOwner = await _unitOfWork.VenueOwnerProfiles.GetIncludeByUserIdAsync(userId);
            if (venueOwner == null)
                throw new Exception("Không tìm thấy chủ địa điểm");

            var voucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId);
            if (voucher == null || voucher.IsDeleted == true)
                throw new Exception("Không tìm thấy voucher");

            if (voucher.VenueOwnerId != venueOwner.Id) 
                throw new Exception("Bạn không có quyền xem lịch sử đổi voucher này");

            if (query.FromDate.HasValue && query.ToDate.HasValue && query.FromDate.Value > query.ToDate.Value)
                throw new Exception("FromDate phải nhỏ hơn hoặc bằng ToDate");

            var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
            var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

            var keyword = query.Keyword?.Trim().ToLower();

            Func<IQueryable<VoucherItem>, IOrderedQueryable<VoucherItem>> orderBy = q =>
                q.OrderByDescending(vi => vi.AcquiredAt);

            if (!string.IsNullOrWhiteSpace(query.OrderBy))
            {
                var order = query.OrderBy?.Trim().ToLower() ?? "desc";

                orderBy = (order) switch
                {
                    ("asc") => q => q.OrderBy(vi => vi.AcquiredAt),
                    ("desc") => q => q.OrderByDescending(vi => vi.AcquiredAt),
                    _ => q => q.OrderByDescending(vi => vi.AcquiredAt)
                };
            }

            var (voucherItems, totalCount) = await _unitOfWork.VoucherItems.GetPagedAsync(
                pageNumber,
                pageSize,
                vi => vi.VoucherId == voucherId
                    && vi.VoucherItemMemberId != null
                    && vi.AcquiredAt != null
                    && vi.IsDeleted == false
                    && (!query.FromDate.HasValue || vi.AcquiredAt >= query.FromDate.Value)
                    && (!query.ToDate.HasValue || vi.AcquiredAt <= query.ToDate.Value)
                    && (
                    string.IsNullOrEmpty(keyword) ||
                    (
                        (vi.ItemCode != null && vi.ItemCode.ToLower().Contains(keyword)) ||
                        (vi.VoucherItemMember != null &&
                             vi.VoucherItemMember.Member != null &&
                             vi.VoucherItemMember.Member.FullName != null &&
                             vi.VoucherItemMember.Member.FullName.ToLower().Contains(keyword)) ||
                        (vi.VoucherItemMember != null &&
                             vi.VoucherItemMember.Member != null &&
                             vi.VoucherItemMember.Member.User != null &&
                             vi.VoucherItemMember.Member.User.Email != null &&
                             vi.VoucherItemMember.Member.User.Email.ToLower().Contains(keyword)) ||
                         (vi.VoucherItemMember != null &&
                             vi.VoucherItemMember.Member != null &&
                             vi.VoucherItemMember.Member.User != null &&
                             vi.VoucherItemMember.Member.User.PhoneNumber != null &&
                             vi.VoucherItemMember.Member.User.PhoneNumber.ToLower().Contains(keyword)
                         )
                    )
                ),
                orderBy,
                q => q.Include(vi => vi.VoucherItemMember).ThenInclude(vim => vim.Member).ThenInclude(m => m.User)
            );

            var response = voucherItems.Select(vi =>
            {
                return new VenueVoucherActivityResponse
                {
                    VoucherId = vi.VoucherId,
                    VoucherItemId = vi.Id,
                    VoucherItemMemberId = vi.VoucherItemMemberId,

                    ItemCode = vi.ItemCode,
                    Status = vi.Status,

                    MemberId = vi.VoucherItemMember?.MemberId,
                    MemberName = vi.VoucherItemMember?.Member?.FullName,
                    MemberEmail = vi.VoucherItemMember?.Member?.User?.Email,
                    MemberPhone = vi.VoucherItemMember?.Member?.User?.PhoneNumber,

                    Quantity = vi.VoucherItemMember?.Quantity ?? 0,
                    TotalPointsUsed = vi.VoucherItemMember?.TotalPointsUsed ?? 0,
                    Note = vi.VoucherItemMember?.Note,

                    AcquiredAt = vi.AcquiredAt,
                    UsedAt = vi.UsedAt,
                    ExpiredAt = vi.ExpiredAt
                };
            }).ToList();

            return new PagedResult<VenueVoucherActivityResponse>
            {
                Items = response,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        private static void ValidateVoucherBeforeSubmit(Voucher voucher)
        {
            if (string.IsNullOrWhiteSpace(voucher.Title))
                throw new Exception("Vui lòng nhập tiêu đề voucher");

            if (string.IsNullOrWhiteSpace(voucher.Description))
                throw new Exception("Vui lòng nhập mô tả voucher");

            if (string.IsNullOrWhiteSpace(voucher.ImageUrl))
                throw new Exception("Vui lòng thêm ảnh voucher");

            if (voucher.VoucherPrice <= 0)
                throw new Exception("Giá voucher phải lớn hơn 0");

            if (!voucher.Quantity.HasValue || voucher.Quantity.Value <= 0)
                throw new Exception("Số lượng phải lớn hơn 0");

            if (!voucher.UsageValidDays.HasValue || voucher.UsageValidDays.Value < 1 || voucher.UsageValidDays.Value > 365)
                throw new Exception("UsageValidDays phải từ 1 đến 365 ngày");

            if (voucher.UsageLimitPerMember.HasValue && voucher.UsageLimitPerMember.Value <= 0)
                throw new Exception("Giới hạn sử dụng mỗi thành viên phải lớn hơn 0");

            if (voucher.StartDate.HasValue && voucher.EndDate.HasValue && voucher.StartDate > voucher.EndDate)
                throw new Exception("Ngày bắt đầu phải trước ngày kết thúc");

            var now = DateTime.UtcNow;
            if (voucher.StartDate.HasValue && voucher.StartDate.Value < now)
                throw new Exception("Ngày bắt đầu không được ở quá khứ");

            if (voucher.EndDate.HasValue && voucher.EndDate.Value < now)
                throw new Exception("Ngày kết thúc không được ở quá khứ");

            if (string.IsNullOrWhiteSpace(voucher.DiscountType))
                throw new Exception("Loại giảm giá không hợp lệ");

            if (voucher.DiscountAmount.HasValue && voucher.DiscountPercent.HasValue)
                throw new Exception("Chỉ được truyền một trong hai: DiscountAmount hoặc DiscountPercent");

            if (voucher.DiscountType == VoucherDiscountType.FIXED_AMOUNT.ToString())
            {
                if (!voucher.DiscountAmount.HasValue || voucher.DiscountAmount.Value <= 0)
                    throw new Exception("Số tiền giảm phải lớn hơn 0");

                if (voucher.DiscountPercent.HasValue)
                    throw new Exception("Không được nhập Percent khi dùng Fixed Amount");
            }
            else if (voucher.DiscountType == VoucherDiscountType.PERCENTAGE.ToString())
            {
                if (!voucher.DiscountPercent.HasValue || voucher.DiscountPercent.Value < 1 || voucher.DiscountPercent.Value > 100)
                    throw new Exception("Phần trăm giảm phải từ 1 đến 100");

                if (voucher.DiscountAmount.HasValue)
                    throw new Exception("Không được nhập Amount khi dùng Percentage");
            }
            else
            {
                throw new Exception("Loại giảm giá không hợp lệ");
            }
        }
    }
}
