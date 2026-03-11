using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;

namespace capstone_backend.Business.Services
{
    public class VoucherItemService : IVoucherItemService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IVoucherCodeGenerator _voucherCodeGenerator;

        public VoucherItemService(IUnitOfWork unitOfWork, IVoucherCodeGenerator voucherCodeGenerator)
        {
            _unitOfWork = unitOfWork;
            _voucherCodeGenerator = voucherCodeGenerator;
        }

        public async Task GenerateVoucherItemsAsync(int voucherId, int quantity)
        {
            var items = new List<VoucherItem>();

            for (int i = 0; i < quantity; i++)
            {
                var code = await _voucherCodeGenerator.GenerateUniqueCodeAsync();

                items.Add(new VoucherItem
                {
                    VoucherId = voucherId,
                    ItemCode = code,
                    Status = VoucherItemStatus.AVAILABLE.ToString(),
                    AcquiredAt = null,
                    UsedAt = null,
                    VoucherItemMemberId = null,
                    IsDeleted = false
                });
            }

            await _unitOfWork.VoucherItems.AddRangeAsync(items);
        }
    }
}
