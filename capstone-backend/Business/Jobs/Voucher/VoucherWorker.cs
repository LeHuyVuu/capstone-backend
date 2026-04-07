
using capstone_backend.Business.Common;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Services;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Jobs.Voucher
{
    public class VoucherWorker : IVoucherWorker
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<VoucherWorker> _logger;
        private readonly IVoucherItemService _voucherItemService;
        private readonly IFcmService? _fcmService;

        public VoucherWorker(IUnitOfWork unitOfWork, ILogger<VoucherWorker> logger, IVoucherItemService voucherItemService, IServiceProvider serviceProvider)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _voucherItemService = voucherItemService;
            _fcmService = serviceProvider.GetService<IFcmService>();
        }

        public async Task ActivateVoucherAsync(int voucherId)  
        {
            var now = DateTime.UtcNow;

            var voucher = await _unitOfWork.Vouchers.GetIncludeByIdAsync(voucherId);
            if (voucher == null || voucher.IsDeleted == true)
                return;

            if (voucher.Status != VoucherStatus.APPROVED.ToString())
                return;

            var inactiveLocations = voucher.VoucherLocations
                .Where(vl => vl.VenueLocation.Status != VenueLocationStatus.ACTIVE.ToString() || vl.VenueLocation.IsDeleted == true)
                .Select(vl => vl.VenueLocation.Name)
                .ToList();

            var inactiveCount = voucher.VoucherLocations
                .Count(vl => vl.VenueLocation.Status != VenueLocationStatus.ACTIVE.ToString() || vl.VenueLocation.IsDeleted == true);

            if (inactiveCount > 0 && inactiveCount == voucher.VoucherLocations.Count)
            {
                _logger.LogWarning($"[Auto-Activate Job] Hủy kích hoạt Voucher {voucherId} vì TẤT CẢ địa điểm đều đã ngưng hoạt động.");
                voucher.Status = VoucherStatus.ENDED.ToString();
                voucher.UpdatedAt = now;

                _unitOfWork.Vouchers.Update(voucher);
                await CleanupJobAsync(voucherId, VoucherJobType.ACTIVATE_VOUCHER.ToString());
                await _unitOfWork.SaveChangesAsync();
                return;
            }

            voucher.Status = VoucherStatus.ACTIVE.ToString();
            voucher.UpdatedAt = now;

            // call create code for voucher item
            await _voucherItemService.GenerateVoucherItemsAsync(voucher.Id, voucher.Quantity.Value);

            // remove job in db for auto publish
            await CleanupJobAsync(voucherId, VoucherJobType.ACTIVATE_VOUCHER.ToString());

            _unitOfWork.Vouchers.Update(voucher);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task EndVoucherAsync(int voucherId)
        {
            var now = DateTime.UtcNow;

            var voucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId);
            if (voucher == null || voucher.IsDeleted == true)
                return;

            if (voucher.Status != VoucherStatus.ACTIVE.ToString())
                return;

            voucher.Status = VoucherStatus.ENDED.ToString();
            voucher.UpdatedAt = now;

            // update all remaining voucher items to end
            await _unitOfWork.VoucherItems.ExecuteUpdateUnassignedVoucherItemsAsync(voucherId);

            // remove job in db for auto end
            await CleanupJobAsync(voucherId, VoucherJobType.END_VOUCHER.ToString());

            _unitOfWork.Vouchers.Update(voucher);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ExpireVoucherItemAsync(int voucherItemId)
        {
            var now = DateTime.UtcNow;

            var voucherItem = await _unitOfWork.VoucherItems.GetByIdAsync(voucherItemId);
            if (voucherItem == null || voucherItem.IsDeleted == true)
                return;

            if (voucherItem.Status != VoucherItemStatus.ACQUIRED.ToString())
                return;

            voucherItem.Status = VoucherItemStatus.EXPIRED.ToString();
            voucherItem.UpdatedAt = now;

            // remove job in db for auto expire
            await CleanupItemJobAsync(voucherItemId, VoucherItemJobType.EXPIRE_VOUCHER_ITEM.ToString());
            _unitOfWork.VoucherItems.Update(voucherItem);
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task CleanupJobAsync(int voucherId, string type)
        {
            var job = await _unitOfWork.VoucherJobs.GetByVoucherIdAndTypeAsync(voucherId, type);
            if (job == null) 
                return;

            _unitOfWork.VoucherJobs.Delete(job);
        }

        private async Task CleanupItemJobAsync(int voucherItemId, string type)
        {
            var job = await _unitOfWork.VoucherItemJobs.GetByVoucherItemIdAndTypeAsync(voucherItemId, type);
            if (job == null)
                return;

            _unitOfWork.VoucherItemJobs.Delete(job);
        }

        public async Task ScanAndRefundInactiveVouchersAsync()
        {
            var vouchersToCheck = await _unitOfWork.Vouchers.GetAsync(
                v => v.Status == VoucherStatus.ACTIVE.ToString() && v.IsDeleted == false,
                include: q => q.Include(v => v.VoucherLocations).ThenInclude(vl => vl.VenueLocation)
            );

            foreach (var voucher in vouchersToCheck)
            {
                var totalLocs = voucher.VoucherLocations.Count;
                var inactiveLocs = voucher.VoucherLocations.Count(vl =>
                    vl.VenueLocation.Status != VenueLocationStatus.ACTIVE.ToString() || vl.VenueLocation.IsDeleted == true);

                if (totalLocs > 0 && inactiveLocs == totalLocs)
                {
                    _logger.LogWarning($"[ScanAndRefundInactiveVouchersAsync Job] Voucher {voucher.Id} has all inactive locations. Processing refund...");

                    await ProcessRefundForVoucherAsync(voucher);
                }
            }
        }

        private async Task ProcessRefundForVoucherAsync(Data.Entities.Voucher voucherEntity)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var itemsToRefund = await _unitOfWork.VoucherItems.GetAsync(
                    vi => vi.VoucherId == voucherEntity.Id && vi.Status == VoucherItemStatus.ACQUIRED.ToString(),
                    include: q => q.Include(vi => vi.VoucherItemMember).ThenInclude(vimm => vimm.Member)
                );

                if (!itemsToRefund.Any())
                    goto EndVoucher;

                foreach (var item in itemsToRefund)
                {
                    if (item.VoucherItemMember == null)
                    {
                        _logger.LogWarning($"VoucherItem {item.Id} has no associated VoucherItemMember. Skipping refund.");
                        continue;
                    }

                    var userId = item.VoucherItemMember.Member.UserId;
                    var points = item.VoucherItemMember.TotalPointsUsed;

                    // 1. Refund points to user's wallet
                    var wallet = await _unitOfWork.Wallets.GetByUserIdAsync(userId);
                    wallet.Points += points;
                    _unitOfWork.Wallets.Update(wallet);

                    // 2. Update item status
                    item.Status = VoucherItemStatus.ENDED.ToString();
                    _unitOfWork.VoucherItems.Update(item);

                    // 3. Create transaction history for refund
                    var transaction = new Transaction
                    {
                        Amount = points.HasValue ? (decimal)points.Value : 0,
                        Currency = "POINTS",
                        UserId = userId,
                        PaymentMethod = PaymentMethod.SYSTEM.ToString(),
                        Description = $"Hoàn tiền voucher '{voucherEntity.Title}' về ví do tất cả địa điểm ngưng hoạt động",
                        DocNo = wallet.Id,
                        TransType = (int)TransactionType.REFUND,
                        ExternalRefCode = null,
                        Status = TransactionStatus.SUCCESS.ToString(),
                    };
                    await _unitOfWork.Transactions.AddAsync(transaction);
                    await _unitOfWork.SaveChangesAsync();

                    // 4. Create Notification
                    var notification = new Data.Entities.Notification
                    {
                        UserId = userId,
                        Title = NotificationTemplate.Voucher.TitleRefundInactiveVoucher,
                        Message = NotificationTemplate.Voucher.GetRefundInactiveVoucherBody(voucherEntity.Title, points.Value.ToString()),
                        Type = NotificationType.Transaction.ToString(),
                        ReferenceType = ReferenceType.WALLET.ToString(),
                        ReferenceId = wallet.Id,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow,
                    };

                    await _unitOfWork.Notifications.AddAsync(notification);

                    // Send FCM notification to user about refund
                    if (_fcmService != null)
                    {
                        var deviceTokens = await _unitOfWork.DeviceTokens.GetTokensByUserId(userId);

                        if (deviceTokens.Any())
                        {
                            _ = _fcmService.SendMultiNotificationAsync(deviceTokens, new DTOs.Notification.SendNotificationRequest
                            {
                                Title = NotificationTemplate.Voucher.TitleRefundInactiveVoucher,
                                Body = NotificationTemplate.Voucher.GetRefundInactiveVoucherBody(voucherEntity.Title, points.ToString()),
                                Data = new Dictionary<string, string>
                                {
                                    { NotificationKeys.Type, NotificationType.Transaction.ToString() },
                                    { NotificationKeys.RefType, ReferenceType.WALLET.ToString() },
                                    { NotificationKeys.RefId, wallet.Id.ToString() },
                                }
                            });
                        }
                    }
                }

                await _unitOfWork.SaveChangesAsync();

                EndVoucher:
                    voucherEntity.Status = VoucherStatus.ENDED.ToString();
                    voucherEntity.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Vouchers.Update(voucherEntity);

                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, $"Lỗi hoàn tiền Voucher {voucherEntity.Id}");
            }
        }

        public async Task EndOutOfStockVoucherAsync()
        {
            var voucher = await _unitOfWork.Vouchers.GetAsync(
                v => v.Status == VoucherStatus.ACTIVE.ToString() && v.IsDeleted == false,
                include: q => q.Include(v => v.VoucherItems)
            );
        }
    }
}
