using AutoMapper;
using capstone_backend.Business.DTOs.Voucher;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using System.Transactions;

namespace capstone_backend.Business.Services
{
    public class VenueVoucherService : IVenueVoucherService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public VenueVoucherService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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

        public async Task<VoucherResponse> SubmitVoucherAsync(int userId, int voucherId)
        {
            var venueOwner = await _unitOfWork.VenueOwnerProfiles.GetIncludeByUserIdAsync(userId);
            if (venueOwner == null)
                throw new Exception("Không tìm thấy chủ địa điểm");

            var voucher = await _unitOfWork.Vouchers.GetIncludeByIdAsync(voucherId);
            if (voucher == null)
                throw new Exception("Không tìm thấy voucher");

            if (voucher.VenueOwnerId != venueOwner.Id)
                throw new Exception("Bạn không có quyền nộp voucher này");

            if (voucher.Status != VoucherStatus.DRAFTED.ToString())
                throw new Exception("Chỉ có thể nộp voucher ở trạng thái DRAFTED");

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
    }
}
