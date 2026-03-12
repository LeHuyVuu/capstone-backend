
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;
using Hangfire;

namespace capstone_backend.Business.Jobs.Voucher
{
    public class VoucherWorker : IVoucherWorker
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<VoucherWorker> _logger;
        private readonly IVoucherItemService _voucherItemService;

        public VoucherWorker(IUnitOfWork unitOfWork, ILogger<VoucherWorker> logger, IVoucherItemService voucherItemService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _voucherItemService = voucherItemService;
        }

        public async Task ActivateVoucherAsync(int voucherId)  
        {
            var now = DateTime.UtcNow;

            var voucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId);
            if (voucher == null || voucher.IsDeleted == true)
                return;

            if (voucher.Status != VoucherStatus.APPROVED.ToString())
                return;

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

        private async Task CleanupJobAsync(int voucherId, string type)
        {
            var job = await _unitOfWork.VoucherJobs.GetByVoucherIdAndTypeAsync(voucherId, type);
            if (job == null) 
                return;

            BackgroundJob.Delete(job.JobId);
        }
    }
}
