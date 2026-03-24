using AutoMapper;
using capstone_backend.Business.DTOs.Accessory;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Services
{
    public class AccessoryService : IAccessoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AccessoryService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<PagedResult<AccessoryResponse>> GetShopAsync(int userId, GetAccessoryShopRequest query)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
            if (couple == null)
                throw new Exception("Member không ở trong couple nào");

            var partnerId = couple.MemberId1 == member.Id ? couple.MemberId2 : couple.MemberId1;

            int pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
            int pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
            var keyword = query.Keyword?.Trim().ToLower();
            var now = DateTime.UtcNow;

            Func<IQueryable<Accessory>, IOrderedQueryable<Accessory>> orderBy = q => q.OrderByDescending(x => x.CreatedAt);

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
                    ("pricepoint", "asc") => q => q.OrderBy(x => x.PricePoint),
                    ("pricepoint", "desc") => q => q.OrderByDescending(x => x.PricePoint),
                    _ => q => q.OrderByDescending(x => x.CreatedAt)
                };
            }

            var (accessories, totalCount) = await _unitOfWork.Accessories.GetPagedAsync(
                pageNumber,
                pageSize,
                a =>
                    a.IsDeleted == false &&
                    a.Status == AccessoryStatus.ACTIVE.ToString() &&
                    (query.Type == null || a.Type == query.Type.ToString()) &&
                    (a.AvailableFrom == null || a.AvailableFrom <= now) &&
                    (a.AvailableTo == null || a.AvailableTo >= now) &&
                    (
                        string.IsNullOrEmpty(keyword) ||
                        a.Name.ToLower().Contains(keyword) ||
                        (a.Description != null && a.Description.ToLower().Contains(keyword))
                    ),
                orderBy
            );

            // TODO: set IsOwnedByMe, IsOwnedByPartner, CanPurchase
            var accessoryIds = accessories.Select(a => a.Id).ToList();
            var ownerRecords = await _unitOfWork.MemberAccessories.GetOwnerAsync(member.Id, partnerId, accessoryIds);

            var myOwnedAccessoryIds = ownerRecords
                .Where(x => x.MemberId == member.Id)
                .Select(x => x.AccessoryId)
                .ToHashSet();

            var partnerOwnedAccessoryIds = ownerRecords
                .Where(x => x.MemberId == partnerId)
                .Select(x => x.AccessoryId)
                .ToHashSet();

            var response = new List<AccessoryResponse>();

            foreach (var accessory in accessories)
            {
                var item = _mapper.Map<AccessoryResponse>(accessory);

                item.IsOwnedByMe = myOwnedAccessoryIds.Contains(accessory.Id);
                item.IsOwnedByPartner = partnerOwnedAccessoryIds.Contains(accessory.Id);

                var isInStock = !accessory.IsLimited.GetValueOrDefault()
                                || accessory.RemainingQuantity == null
                                || accessory.RemainingQuantity > 0;

                item.CanPurchase =
                    !item.IsOwnedByMe &&
                    !item.IsOwnedByPartner &&
                    isInStock &&
                    accessory.Status == AccessoryStatus.ACTIVE.ToString() &&
                    (accessory.AvailableFrom == null || accessory.AvailableFrom <= now) &&
                    (accessory.AvailableTo == null || accessory.AvailableTo >= now);

                response.Add(item);
            }

            return new PagedResult<AccessoryResponse>
            {
                Items = response,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<AccessoryDetailResponse?> GetDetailAsync(int userId, int accessoryId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
            if (couple == null)
                throw new Exception("Member không ở trong couple nào");

            var partnerId = couple.MemberId1 == member.Id ? couple.MemberId2 : couple.MemberId1;

            var accessory = await _unitOfWork.Accessories.GetByIdAsync(accessoryId);
            if (accessory == null || accessory.IsDeleted == true || accessory.Status != AccessoryStatus.ACTIVE.ToString())
                return null;

            var response = _mapper.Map<AccessoryDetailResponse>(accessory);

            var ownerRecords = await _unitOfWork.MemberAccessories.GetOwnerAsync(member.Id, partnerId, new List<int> { accessoryId });
            response.IsOwnedByMe = ownerRecords.Any(x => x.MemberId == member.Id);
            response.IsOwnedByPartner = ownerRecords.Any(x => x.MemberId == partnerId);

            var isInStock = !accessory.IsLimited.GetValueOrDefault()
                            || accessory.RemainingQuantity == null
                            || accessory.RemainingQuantity > 0;

            response.CanPurchase =
                !response.IsOwnedByMe &&
                isInStock &&
                accessory.Status == AccessoryStatus.ACTIVE.ToString() &&
                (accessory.AvailableFrom == null || accessory.AvailableFrom <= DateTime.UtcNow) &&
                (accessory.AvailableTo == null || accessory.AvailableTo >= DateTime.UtcNow);

            return response;
        }

        public async Task<PurchaseResponse> PurchaseAccessoryAsync(int userId, int accessoryId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
            if (couple == null)
                throw new Exception("Member không ở trong couple nào");

            var partnerId = couple.MemberId1 == member.Id ? couple.MemberId2 : couple.MemberId1;

            var accessory = await _unitOfWork.Accessories.GetByIdAsync(accessoryId);
            if (accessory == null || accessory.IsDeleted == true)
                throw new Exception("Phụ kiện không tồn tại");

            if (accessory.Status != AccessoryStatus.ACTIVE.ToString())
                throw new Exception("Phụ kiện hiện không khả dụng");

            var now = DateTime.UtcNow;

            if (accessory.AvailableFrom != null && accessory.AvailableFrom > now)
                throw new Exception("Phụ kiện chưa được mở bán");

            if (accessory.AvailableTo != null && accessory.AvailableTo < now)
                throw new Exception("Phụ kiện đã ngừng bán");

            var ownedAccessories = await _unitOfWork.MemberAccessories
                .GetOwnerAsync(member.Id, partnerId, new List<int> { accessoryId });

            if (ownedAccessories.Any())
                throw new Exception("Phụ kiện này đã được sở hữu trong couple");

            if (couple.TotalPoints < accessory.PricePoint)
                throw new Exception("Bạn không đủ Couple Point để mua phụ kiện này");

            if (accessory.IsLimited.GetValueOrDefault())
            {
                if (!accessory.RemainingQuantity.HasValue || accessory.RemainingQuantity.Value <= 0)
                    throw new Exception("Phụ kiện đã hết hàng");
            }

            var purchase = new AccessoryPurchase();

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Create purchase record
                purchase = new AccessoryPurchase
                {
                    AccessoryId = accessoryId,
                    CoupleId = couple.id,
                    PricePoint = accessory.PricePoint.Value,
                    PurchasedByMemberId = member.Id,
                    Status = AccessoryPurchaseStatus.SUCCESS.ToString(),
                    CreatedAt = now
                };

                await _unitOfWork.AccessoryPurchases.AddAsync(purchase);
                await _unitOfWork.SaveChangesAsync();

                // Grant accessory to buyer and partner
                var memberAccessories = new List<MemberAccessory>
                {
                    new MemberAccessory
                    {
                        AccessoryId = accessoryId,
                        MemberId = member.Id,
                        PurchaseId = purchase.Id,
                        AcquiredAt = now,
                        ExpiredAt = null,
                        IsEquipped = false
                    },

                    new MemberAccessory
                    {
                        AccessoryId = accessoryId,
                        MemberId = partnerId,
                        PurchaseId = purchase.Id,
                        AcquiredAt = now,
                        ExpiredAt = null,
                        IsEquipped = false
                    }
                };

                await _unitOfWork.MemberAccessories.AddRangeAsync(memberAccessories);

                // Deduct couple points
                couple.TotalPoints = Math.Max(0, couple.TotalPoints.Value - accessory.PricePoint.Value);
                _unitOfWork.CoupleProfiles.Update(couple);

                // Decrease remaining quantity if limited
                if (accessory.IsLimited.GetValueOrDefault() && accessory.RemainingQuantity.HasValue)
                {
                    accessory.RemainingQuantity = Math.Max(0, accessory.RemainingQuantity.Value - 1);
                    _unitOfWork.Accessories.Update(accessory);
                    await _unitOfWork.SaveChangesAsync();
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            return new PurchaseResponse
            {
                AccessoryId = accessoryId,
                Code = accessory.Code,
                Name = accessory.Name,
                PricePoint = accessory.PricePoint.Value,
                CouplePointRemaining = couple.TotalPoints.Value,
                GrantedMemberIds = new List<int> { member.Id, partnerId },
                PurchaseId = purchase.Id,
                PurchasedAt = purchase.CreatedAt
            };
        }

        public async Task<PagedResult<MyAccessoryResponse>> GetMyAccessoryAsync(int userId, GetMyAccessoryRequest query)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            int pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
            int pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
            var keyword = query.Keyword?.Trim().ToLower();
            var now = DateTime.UtcNow;

            Func<IQueryable<MemberAccessory>, IOrderedQueryable<MemberAccessory>> orderBy = q => q.OrderByDescending(x => x.AcquiredAt);

            if (!string.IsNullOrWhiteSpace(query.SortBy))
            {
                var sortBy = query.SortBy.Trim().ToLower();
                var order = query.OrderBy?.Trim().ToLower() ?? "desc";

                orderBy = (sortBy, order) switch
                {
                    ("acquiredat", "asc") => q => q.OrderBy(x => x.AcquiredAt),
                    ("acquiredat", "desc") => q => q.OrderByDescending(x => x.AcquiredAt),
                    ("name", "asc") => q => q.OrderBy(x => x.Accessory.Name),
                    ("name", "desc") => q => q.OrderByDescending(x => x.Accessory.Name),
                    _ => q => q.OrderByDescending(x => x.AcquiredAt)
                };
            }

            var (memberAccessories, totalCount) = await _unitOfWork.MemberAccessories.GetPagedAsync(
                pageNumber,
                pageSize,
                ma =>
                    ma.MemberId == member.Id &&
                    (query.EquippedOnly == false || ma.IsEquipped == query.EquippedOnly) &&
                    (query.Type == null || ma.Accessory.Type == query.Type.ToString()) &&
                    (
                        string.IsNullOrEmpty(keyword) ||
                        ma.Accessory.Code.ToLower().Contains(keyword) ||
                        ma.Accessory.Name.ToLower().Contains(keyword) ||
                     (ma.Accessory.Description != null && ma.Accessory.Description.ToLower().Contains(keyword))),
                orderBy,
                ma => ma.Include(ma => ma.Accessory)
            );

            var response = _mapper.Map<List<MyAccessoryResponse>>(memberAccessories);

            return new PagedResult<MyAccessoryResponse>
            {
                Items = response,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<EquipAccessoryResponse> EquipAccessoryAsync(int userId, int memberAccessoryId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var memberAccessory = await _unitOfWork.MemberAccessories.GetByIdAsync(memberAccessoryId);
            if (memberAccessory == null || memberAccessory.MemberId != member.Id)
                throw new Exception("Phụ kiện của thành viên không tồn tại");

            if (memberAccessory.ExpiredAt != null && memberAccessory.ExpiredAt <= DateTime.UtcNow)
                throw new Exception("Phụ kiện này đã hết hạn sử dụng");

            if (memberAccessory.Accessory == null || memberAccessory.Accessory.IsDeleted == true)
                throw new Exception("Phụ kiện không tồn tại");

            if (memberAccessory.IsEquipped == true)
                throw new Exception("Phụ kiện đã được trang bị");

            var equippedAccessories = await _unitOfWork.MemberAccessories.GetEquippedByMemberIdAndTypeAsync(member.Id, memberAccessory.Accessory.Type, memberAccessory.Id);

            if (equippedAccessories.Any())
            {
                foreach (var item in equippedAccessories)
                {
                    item.IsEquipped = false;
                }

                _unitOfWork.MemberAccessories.UpdateRange(equippedAccessories);
            }

            memberAccessory.IsEquipped = true;
            _unitOfWork.MemberAccessories.Update(memberAccessory);  
            await _unitOfWork.SaveChangesAsync();

            return new EquipAccessoryResponse
            {
                MemberAccessoryId = memberAccessory.Id,
                AccessoryId = memberAccessory.AccessoryId.Value,
                Code = memberAccessory.Accessory?.Code,
                Name = memberAccessory.Accessory?.Name,
                Type = memberAccessory.Accessory?.Type,
                IsEquipped = true
            };
        }

        public async Task<EquipAccessoryResponse> UnequipAccessoryAsync(int userId, int memberAccessoryId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var memberAccessory = await _unitOfWork.MemberAccessories.GetByIdAsync(memberAccessoryId);
            if (memberAccessory == null || memberAccessory.MemberId != member.Id)
                throw new Exception("Phụ kiện của thành viên không tồn tại");

            if (memberAccessory.Accessory == null || memberAccessory.Accessory.IsDeleted == true)
                throw new Exception("Phụ kiện không tồn tại");

            if (!memberAccessory.AccessoryId.HasValue)
                throw new Exception("Phụ kiện không hợp lệ");

            if (memberAccessory.IsEquipped != true)
                throw new Exception("Phụ kiện chưa được trang bị");

            memberAccessory.IsEquipped = false;
            _unitOfWork.MemberAccessories.Update(memberAccessory);
            await _unitOfWork.SaveChangesAsync();

            return new EquipAccessoryResponse
            {
                MemberAccessoryId = memberAccessory.Id,
                AccessoryId = memberAccessory.AccessoryId.Value,
                Code = memberAccessory.Accessory.Code,
                Name = memberAccessory.Accessory.Name,
                Type = memberAccessory.Accessory.Type,
                IsEquipped = false
            };
        }

        public async Task<MyAccessoryDetailResponse> GetMyAccessoryDetailAsync(int userId, int memberAccessoryId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var memberAccessory = await _unitOfWork.MemberAccessories.GetByIdAsync(memberAccessoryId);
            if (memberAccessory == null || memberAccessory.MemberId != member.Id)
                throw new Exception("Phụ kiện của thành viên không tồn tại");

            if (memberAccessory.Accessory == null || memberAccessory.Accessory.IsDeleted == true)
                throw new Exception("Phụ kiện không tồn tại");

            var response = _mapper.Map<MyAccessoryDetailResponse>(memberAccessory);
            return response;
        }

        public async Task<PagedResult<PurchaseHistoryResponse>> GetPurchaseHistoriesAsync(int userId, GetPurchaseHistoryRequest query)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
            if (couple == null)
                throw new Exception("Member không ở trong couple nào");

            int pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
            int pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
            var keyword = query.Keyword?.Trim().ToLower();

            Func<IQueryable<AccessoryPurchase>, IOrderedQueryable<AccessoryPurchase>> orderBy = q => q.OrderByDescending(x => x.CreatedAt);
            if (!string.IsNullOrWhiteSpace(query.SortBy))
            {
                var sortBy = query.SortBy.Trim().ToLower();
                var order = query.OrderBy?.Trim().ToLower() ?? "desc";
                orderBy = (sortBy, order) switch
                {
                    ("createdat", "asc") => q => q.OrderBy(x => x.CreatedAt),
                    ("createdat", "desc") => q => q.OrderByDescending(x => x.CreatedAt),
                    _ => q => q.OrderByDescending(x => x.CreatedAt)
                };
            }
            var (purchases, totalCount) = await _unitOfWork.AccessoryPurchases.GetPagedAsync(
                pageNumber,
                pageSize,
                ap => (ap.PurchasedByMemberId == member.Id &&
                    (query.FromDate == null || ap.CreatedAt >= query.FromDate) &&
                    (query.ToDate == null || ap.CreatedAt <= query.ToDate) &&
                    (
                        string.IsNullOrEmpty(keyword) ||
                        ap.Accessory.Code.ToLower().Contains(keyword) ||
                        ap.Accessory.Name.ToLower().Contains(keyword) ||
                        (ap.Accessory.Description != null && ap.Accessory.Description.ToLower().Contains(keyword))
                    )),
                orderBy,
                ap => ap.Include(ap => ap.Accessory).Include(ap => ap.PurchasedByMember)
            );
            var response = _mapper.Map<List<PurchaseHistoryResponse>>(purchases);
            return new PagedResult<PurchaseHistoryResponse>
            {
                Items = response,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<List<EquippedAccessoryBriefResponse>> GetEquippedAccessoryForMemberAsync(int memberId)
        {
            var equippedAccessories = await _unitOfWork.MemberAccessories.GetEquippedByMemberIdAsync(memberId);

            var response = _mapper.Map<List<EquippedAccessoryBriefResponse>>(equippedAccessories);
            return response;
        }
    }
}
