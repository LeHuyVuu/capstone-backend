using AutoMapper;
using capstone_backend.Business.DTOs.Accessory;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;

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
    }
}
