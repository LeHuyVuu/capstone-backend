using AutoMapper;
using capstone_backend.Business.DTOs.Challenge;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.Voucher;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Jobs.Voucher;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using NanoidDotNet;

namespace capstone_backend.Business.Services
{
    public class MemberVoucherService : IMemberVoucherService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<MemberVoucherService> _logger;
        private readonly IQrCodeService _qrCodeService;
        private readonly S3StorageService _s3Service;

        public MemberVoucherService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<MemberVoucherService> logger, IQrCodeService qrCodeService, S3StorageService s3Service)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _qrCodeService = qrCodeService;
            _s3Service = s3Service;
        }

        public async Task<ExchangeVoucherResponse> ExchangeVoucherAsync(int userId, ExchangeVoucherRequest request)
        {
            // 1. Validate request
            if (request == null || request.Items == null || !request.Items.Any())
                throw new Exception("Danh sách voucher đổi không được để trống");

            if (request.Items.Any(x => x.VoucherId <= 0 || x.Quantity <= 0))
                throw new Exception("VoucherId hoặc số lượng không hợp lệ");

            // Group by VoucherId to sum quantities for the same voucher
            var groupedItems = request.Items
                .GroupBy(x => x.VoucherId)
                .Select(g => new ExchangeVoucherItemRequest
                {
                    VoucherId = g.Key,
                    Quantity = g.Sum(x => x.Quantity)
                }).ToList();

            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Không tìm thấy thông tin thành viên");

            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
            if (couple == null)
                throw new Exception("Không tìm thấy thông tin cặp đôi");

            var wallet = await _unitOfWork.Wallets.GetByUserIdAsync(userId);
            if (wallet == null)
                await _unitOfWork.Wallets.AddAsync(new Wallet
                {
                    UserId = userId,
                    Balance = 0,
                    Points = 0,
                    IsActive = true,
                });

            var now = DateTime.UtcNow;

            // 2. Get all vouchers
            var voucherIds = groupedItems.Select(x => x.VoucherId).Distinct().ToList();
            var vouchers = await _unitOfWork.Vouchers.GetByIdsWithItemsAsync(voucherIds);

            if (vouchers.ToList().Count != voucherIds.Count)
                throw new Exception("Có voucher không tồn tại");

            int totalPointsRequired = 0;
            int totalQuantity = 0;

            var voucherMap = vouchers.ToDictionary(v => v.Id, v => v);

            // 3. Validate each voucher
            foreach (var reqItem in groupedItems)
            {
                var voucher = voucherMap[reqItem.VoucherId];

                if (voucher.IsDeleted == true)
                    throw new Exception($"Voucher '{voucher.Title}' không tồn tại");

                if (voucher.Status != VoucherStatus.ACTIVE.ToString())
                    throw new Exception($"Voucher '{voucher.Title}' hiện không khả dụng");

                if (voucher.StartDate.HasValue && voucher.StartDate.Value > now)
                    throw new Exception($"Voucher '{voucher.Title}' chưa đến thời gian áp dụng");

                if (voucher.EndDate.HasValue && voucher.EndDate.Value < now)
                    throw new Exception($"Voucher '{voucher.Title}' đã hết thời gian đổi");

                if (voucher.PointPrice <= 0)
                    throw new Exception($"Voucher '{voucher.Title}' chưa được cấu hình điểm đổi");

                // Check remaining quantity
                var availableCount = voucher.VoucherItems.Count(vi =>
                    vi.IsDeleted == false &&
                    vi.VoucherItemMemberId == null &&
                    vi.Status == VoucherItemStatus.AVAILABLE.ToString()
                );

                if (availableCount < reqItem.Quantity)
                    throw new Exception($"Voucher '{voucher.Title}' chỉ còn {availableCount} mã, không đủ để đổi");

                if (voucher.UsageLimitPerMember.HasValue && voucher.UsageLimitPerMember > 0)
                {
                    var memberUsedCount = await _unitOfWork.VoucherItems.CountMemberAcquiredVoucherAsync(member.Id, voucher.Id);
                    if (memberUsedCount + reqItem.Quantity > voucher.UsageLimitPerMember.Value)
                        throw new Exception($"Bạn đã đổi voucher '{voucher.Title}' {memberUsedCount} lần, chỉ được đổi tối đa {voucher.UsageLimitPerMember.Value} lần");
                }

                totalPointsRequired += voucher.PointPrice * reqItem.Quantity;
                totalQuantity += reqItem.Quantity;
            }

            // 4. Validate points
            var currentPoints = wallet.Points ?? 0;
            if (currentPoints < totalPointsRequired)
                throw new Exception($"Bạn cần {totalPointsRequired} điểm để đổi các voucher này, nhưng bạn chỉ có {currentPoints} điểm");

            // 5. Transaction
            await _unitOfWork.BeginTransactionAsync();
            VoucherItemMember voucherItemMember;
            var exchangedVoucherItems = new List<VoucherItem>();
            var expireSchedules = new List<(int VoucherItemId, DateTime ExpiredAt)>();
            try
            {
                // Deduct points
                wallet.Points = currentPoints - totalPointsRequired;
                wallet.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Wallets.Update(wallet);

                // Create transaction record (voucher item member)
                voucherItemMember = new VoucherItemMember
                {
                    MemberId = member.Id,
                    Quantity = totalQuantity,
                    TotalPointsUsed = totalPointsRequired,
                    Note = request.Note
                };

                await _unitOfWork.VoucherItemMembers.AddAsync(voucherItemMember);
                await _unitOfWork.SaveChangesAsync();

                // Assign voucher items member for each voucher item
                foreach (var reqItem in groupedItems)
                {
                    var voucher = voucherMap[reqItem.VoucherId];

                    // Get available voucher items for this voucher
                    var availableVoucherItems = await _unitOfWork.VoucherItems.GetAvailableVoucherItemsForExchangeAsync(voucher.Id, reqItem.Quantity);

                    if (availableVoucherItems.ToList().Count < reqItem.Quantity)
                        throw new Exception($"Voucher '{voucher.Title}' đã hết số lượng");

                    foreach (var voucherItem in availableVoucherItems)
                    {
                        voucherItem.VoucherItemMemberId = voucherItemMember.Id;
                        voucherItem.Status = VoucherItemStatus.ACQUIRED.ToString();
                        voucherItem.AcquiredAt = now;
                        voucherItem.UpdatedAt = now;

                        if (voucher.UsageValidDays.HasValue && voucher.UsageValidDays.Value > 0)
                        {
                            voucherItem.ExpiredAt = now.AddDays(voucher.UsageValidDays.Value);
                            // TODO: Add expire job for this voucher item
                            expireSchedules.Add((voucherItem.Id, voucherItem.ExpiredAt.Value));
                        }

                        _unitOfWork.VoucherItems.Update(voucherItem);
                        exchangedVoucherItems.Add(voucherItem);
                    }

                    // Deduct remaining quantity for the voucher
                    voucher.RemainingQuantity = (voucher.RemainingQuantity ?? 0) - reqItem.Quantity;
                    voucher.UpdatedAt = now;
                    _unitOfWork.Vouchers.Update(voucher);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            // Gen qrCode
            try
            {
                var uploadTasks = exchangedVoucherItems.Select(async item =>
                {
                    var qrBytes = await Task.Run(() => _qrCodeService.GenerateQrWithLogoAsync(item.ItemCode));

                    // upload to s3
                    var fileName = $"vouchers/qr_{item.Id}_{Guid.NewGuid():N}.png";
                    var s3Url = await _s3Service.UploadBytesAsync(qrBytes, fileName);

                    item.QrCodeUrl = s3Url;
                });

                // Wait for all uploads to complete
                await Task.WhenAll(uploadTasks);

                // Update voucher items with qr code url
                _unitOfWork.VoucherItems.UpdateRange(exchangedVoucherItems);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exchange success but error at gen qrCode {Id}", voucherItemMember.Id);
            }

            var voucherItemJobs = new List<VoucherItemJob>();

            // Schedule after commit
            foreach (var item in expireSchedules)
            {
                var delay = item.ExpiredAt - DateTime.UtcNow;
                var jobId = string.Empty;

                if (delay <= TimeSpan.Zero)
                {
                    jobId = BackgroundJob.Enqueue<IVoucherWorker>(job =>
                        job.ExpireVoucherItemAsync(item.VoucherItemId));
                }
                else
                {
                    jobId = BackgroundJob.Schedule<IVoucherWorker>(job =>
                        job.ExpireVoucherItemAsync(item.VoucherItemId), delay);
                }
                voucherItemJobs.Add(new VoucherItemJob
                {
                    VoucherItemId = item.VoucherItemId,
                    JobId = jobId,
                    JobType = VoucherItemJobType.EXPIRE_VOUCHER_ITEM.ToString()
                });
            }

            await _unitOfWork.VoucherItemJobs.AddRangeAsync(voucherItemJobs);
            await _unitOfWork.SaveChangesAsync();

            // Map response
            foreach (var voucherItem in exchangedVoucherItems)
            {
                var voucher = voucherMap[voucherItem.VoucherId];
                voucherItem.Voucher = voucher; // Ensure Voucher is loaded for mapping
            }

            return new ExchangeVoucherResponse
            {
                VoucherItemMemberId = voucherItemMember.Id,
                MemberId = member.Id,
                TotalQuantityExchanged = totalQuantity,
                TotalPointsUsed = totalPointsRequired,
                RemainingPoints = couple.TotalPoints ?? 0,
                CreatedAt = voucherItemMember.CreatedAt ?? DateTime.UtcNow,
                VoucherItems = _mapper.Map<List<ExchangeVoucherItemResult>>(exchangedVoucherItems)
            };
        }

        public async Task<MemberVoucherDetailResponse> GetMemberVoucherByIdAsync(int voucherId)
        {
            var voucher = await _unitOfWork.Vouchers.GetIncludeByIdAsync(voucherId);
            if (voucher == null || voucher.Status != VoucherStatus.ACTIVE.ToString())
                throw new Exception("Không tìm thấy voucher");

            var response = _mapper.Map<MemberVoucherDetailResponse>(voucher);
            if ((voucher.RemainingQuantity ?? 0) <= 0)
            {
                response.IsAvailable = false;
                response.UnavailableReason = "Voucher đã hết số lượng";
                return response;
            }

            response.IsAvailable = true;
            response.UnavailableReason = null;
            return response;
        }

        public async Task<PagedResult<MemberVoucherListItemResponse>> GetMemberVouchersAsync(int userId, GetMemberVouchersRequest request)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Không tìm thấy hồ sơ thành viên");

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

            var voucherIds = vouchers.Select(v => v.Id).ToList();
            var memberVoucherAcquiredCounts = await _unitOfWork.VoucherItems.CountMemberAcquiredVouchersAsync(member.Id, voucherIds);

            var response = _mapper.Map<List<MemberVoucherListItemResponse>>(vouchers);

            foreach (var item in response)
            {
                var usedCount = memberVoucherAcquiredCounts.TryGetValue(item.Id, out var count) ? count : 0;

                if (item.UsageLimitPerMember.HasValue)
                    item.RemainingUsagePerMember = Math.Max(0, item.UsageLimitPerMember.Value - usedCount);
                else
                    item.RemainingUsagePerMember = null;
            }

            return new PagedResult<MemberVoucherListItemResponse>
            {
                Items = response,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<MemberVoucherItemResponse>> GetMyVouchersAsync(int userId, GetMyVouchersRequest request)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Không tìm thấy thông tin thành viên");

            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

            var keyword = request.Keyword?.Trim().ToLower();

            // Create order
            Func<IQueryable<VoucherItem>, IOrderedQueryable<VoucherItem>> orderBy = q =>
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
                    ("accquiredat", "asc") => q => q.OrderBy(x => x.AcquiredAt),
                    ("accquiredat", "desc") => q => q.OrderByDescending(x => x.AcquiredAt),
                    _ => q => q.OrderByDescending(x => x.CreatedAt) // Default order
                };
            }

            var allowedStatuses = new List<string>
            {
                VoucherItemStatus.ACQUIRED.ToString(),
                VoucherItemStatus.USED.ToString(),
                VoucherItemStatus.EXPIRED.ToString()
            };

            var (voucherItems, totalCount) = await _unitOfWork.VoucherItems.GetPagedAsync(
                    pageNumber,
                    pageSize,
                    vi => vi.IsDeleted == false && 
                          (request.Status != null ? vi.Status == request.Status.ToString() : allowedStatuses.Contains(vi.Status)) &&
                          vi.VoucherItemMember != null &&
                          vi.VoucherItemMember.MemberId == member.Id &&
                          (!request.VoucherId.HasValue || vi.VoucherId == request.VoucherId.Value) &&
                          (string.IsNullOrEmpty(keyword) || (
                            vi.ItemCode != null && vi.ItemCode.ToLower().Contains(keyword) ||
                            vi.Voucher.Title != null && vi.Voucher.Title.ToLower().Contains(keyword) ||
                            vi.Voucher.Description != null && vi.Voucher.Description.ToLower().Contains(keyword)
                         )),
                    orderBy,
                    vi => vi.Include(vi => vi.Voucher)
                );

            var response = _mapper.Map<List<MemberVoucherItemResponse>>(voucherItems);
            foreach (var item in response)
            {
                var voucher = voucherItems.FirstOrDefault(vi => vi.VoucherId == item.VoucherId)?.Voucher;
                if (voucher != null)
                {
                    item.DiscountType = voucher.DiscountType;
                    item.DiscountAmount = voucher.DiscountAmount;
                    item.DiscountPercent = voucher.DiscountPercent;
                }
            }

            return new PagedResult<MemberVoucherItemResponse>
            {
                Items = response,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<MemberVoucherItemDetailResponse> GetMyVoucherDetailsAsync(int userId, int voucherItemId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Không tìm thấy thông tin thành viên");

            var voucherItem = await _unitOfWork.VoucherItems.GetIncludeByIdAsync(voucherItemId);
            if (voucherItem == null)
                throw new Exception("Không tìm thấy voucher item");

            if (voucherItem.VoucherItemMember == null || voucherItem.VoucherItemMember.MemberId != member.Id)
                throw new Exception("Bạn không có quyền truy cập voucher này");

            var response = _mapper.Map<MemberVoucherItemDetailResponse>(voucherItem);
            return response;
        }

        public async Task<PagedResult<MemberVoucherTransactionListItemResponse>> GetMemberVoucherTransactionsAsync(int userId, GetMemberVoucherTransactionsRequest request)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Không tìm thấy thông tin thành viên");

            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

            var keyword = request.Keyword?.Trim().ToLower();

            // Create order
            Func<IQueryable<VoucherItemMember>, IOrderedQueryable<VoucherItemMember>> orderBy = q =>
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

            var (transactions, totalCount) = await _unitOfWork.VoucherItemMembers.GetPagedAsync(
                pageNumber,
                pageSize,
                vim => vim.MemberId == member.Id &&
                       (!request.FromDate.HasValue || vim.CreatedAt >= request.FromDate.Value) &&
                       (!request.ToDate.HasValue || vim.CreatedAt <= request.ToDate.Value) &&
                       (string.IsNullOrEmpty(keyword) || (
                            vim.VoucherItems.Any(vi => vi.ItemCode != null && vi.ItemCode.ToLower().Contains(keyword)) ||
                            vim.VoucherItems.Any(vi => vi.Voucher.Title != null && vi.Voucher.Title.ToLower().Contains(keyword)) ||
                            vim.VoucherItems.Any(vi => vi.Voucher.Description != null && vi.Voucher.Description.ToLower().Contains(keyword))
                       )),
                orderBy,
                vim => vim.Include(vim => vim.VoucherItems).ThenInclude(vi => vi.Voucher)
            );

            var response = _mapper.Map<List<MemberVoucherTransactionListItemResponse>>(transactions);

            return new PagedResult<MemberVoucherTransactionListItemResponse>
            {
                Items = response,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<MemberVoucherTransactionDetailResponse> GetMemberVoucherTransactionDetailsAsync(int userId, int voucherItemMemberId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Không tìm thấy thông tin thành viên");

            var transaction = await _unitOfWork.VoucherItemMembers.GetIncludeByIdAsync(member.Id, voucherItemMemberId);
            if (transaction == null)
                throw new Exception("Không tìm thấy giao dịch voucher");

            var response = _mapper.Map<MemberVoucherTransactionDetailResponse>(transaction);
            return response;
        }
    }
}
