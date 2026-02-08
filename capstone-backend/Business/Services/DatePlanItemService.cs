using AutoMapper;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.DatePlanItem;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Extensions.Common;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Services
{
    public class DatePlanItemService : IDatePlanItemService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public DatePlanItemService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<int> AddVenuesToDatePlanAsync(int userId, int datePlanId, CreateDatePlanItemRequest request)
        {
            try
            {
                var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
                if (member == null)
                    throw new Exception("Member not found");

                var couple = await _unitOfWork.CoupleProfiles.GetByMemberIdAsync(member.Id);
                if (couple == null)
                    throw new Exception("Member does not belong to any couples");

                var datePlan = await _unitOfWork.DatePlans.GetByIdAndCoupleIdAsync(datePlanId, couple.id);
                if (datePlan == null)
                    throw new Exception("Date plan not found");

                // Check if date plan start and end time are valid
                var now = DateTime.UtcNow;
                if (!datePlan.PlannedStartAt.HasValue || !datePlan.PlannedEndAt.HasValue)
                    throw new Exception("Date plan start and end time must be set before adding venues");

                if (datePlan.PlannedStartAt.Value < now)
                    throw new Exception("Date plan start time must be in the future");

                // Snapshot
                var venues = request.Venues ?? new List<DatePlanItemRequest>();
                if (venues == null || !venues.Any())
                    throw new Exception("Venues cannot be empty");

                // Check if only distinct venue locations are added
                var datePlanItems = await _unitOfWork.DatePlanItems.GetByDatePlanIdAsync(datePlanId);
                var existingVenueIds = datePlanItems.Select(dpi => dpi.VenueLocationId).ToHashSet();

                var requestVenueIds = request.Venues
                    .Select(x => x.VenueLocationId)
                    .ToList();

                var existedIds = requestVenueIds
                    .Where(id => existingVenueIds.Contains(id))
                    .Distinct()
                    .ToList();

                if (requestVenueIds.Count != requestVenueIds.Distinct().Count())
                    throw new Exception("Duplicate venue locations in request");

                if (requestVenueIds.Any(id => existingVenueIds.Contains(id)))
                    throw new Exception($"Venue locations already exist in this date plan: {string.Join(", ", existedIds)}");

                var venuesWithTime = venues
                    .Where(v => v.StartTime.HasValue && v.EndTime.HasValue)
                    .ToList();

                for (int i = 0; i < venuesWithTime.Count; i++)
                {
                    for (int j = i + 1; j < venuesWithTime.Count; j++)
                    {
                        var venue1 = venuesWithTime[i];
                        var venue2 = venuesWithTime[j];

                        // Check if time ranges overlap
                        if (venue1.StartTime < venue2.EndTime && venue2.StartTime < venue1.EndTime)
                        {
                            throw new Exception($"Time slots overlap between venues: {venue1.StartTime:HH:mm}-{venue1.EndTime:HH:mm} and {venue2.StartTime:HH:mm}-{venue2.EndTime:HH:mm}");
                        }
                    }
                }

                var existingItemsWithTime = datePlanItems
                    .Where(dpi => dpi.StartTime.HasValue && dpi.EndTime.HasValue && dpi.IsDeleted == false)
                    .ToList();

                foreach (var newVenue in venuesWithTime)
                {
                    foreach (var existingItem in existingItemsWithTime)
                    {
                        if (newVenue.StartTime < existingItem.EndTime && existingItem.StartTime < newVenue.EndTime)
                        {
                            throw new Exception($"Time slot {newVenue.StartTime:HH:mm}-{newVenue.EndTime:HH:mm} overlaps with existing item: {existingItem.StartTime:HH:mm}-{existingItem.EndTime:HH:mm}");
                        }
                    }
                }

                var maxOderIndex = datePlanItems
                    .Select(x => x.OrderIndex)
                    .Max() ?? 0;

                var items = new List<DatePlanItem>();

                var planStartVn = TimezoneUtil.ToVietNamTime(datePlan.PlannedStartAt!.Value);
                var planEndVn = TimezoneUtil.ToVietNamTime(datePlan.PlannedEndAt!.Value);

                foreach (var v in venues)
                {

                    var (itemStartVn, itemEndVn) = ResolveItemRangeWithinPlan(
                        planStartVn, planEndVn,
                        v.StartTime.Value, v.EndTime.Value
                    );

                    var item = _mapper.Map<DatePlanItem>(v);
                    item.DatePlanId = datePlanId;

                    item.OrderIndex = ++maxOderIndex;
                    items.Add(item);

                    datePlan.TotalCount = datePlan.TotalCount += 1;
                };

                _unitOfWork.DatePlans.Update(datePlan);
                await _unitOfWork.DatePlanItems.AddRangeAsync(items);
                return await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }

        private static (DateTime Start, DateTime End) ResolveItemRangeWithinPlan(
            DateTime planStartVn,
            DateTime planEndVn,
            TimeOnly startTime,
            TimeOnly endTime)
        {
            var day0 = DateOnly.FromDateTime(planStartVn);
            var candidates = new[]
            {
                day0.ToDateTime(startTime),
                day0.AddDays(1).ToDateTime(startTime)
            };

            foreach (var start in candidates)
            {
                var end = DateOnly.FromDateTime(start).ToDateTime(endTime);

                if (endTime < startTime)
                    end = end.AddDays(1);

                if (start >= planStartVn && end <= planEndVn && end > start)
                    return (start, end);
            }

            throw new Exception("Item time is outside date plan time range");
        }


        public async Task<int> DeleteDatePlanItemAsync(int value, int datePlanItemId, int datePlanId)
        {
            try
            {
                var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(value);
                if (member == null)
                    throw new Exception("Member not found");

                var couple = await _unitOfWork.CoupleProfiles.GetByMemberIdAsync(member.Id);
                if (couple == null)
                    throw new Exception("Member does not belong to any couples");

                var datePlan = await _unitOfWork.DatePlans.GetByIdAndCoupleIdAsync(datePlanId, couple.id);
                if (datePlan == null)
                    throw new Exception("Date plan not found");

                var datePlanItem = await _unitOfWork.DatePlanItems.GetByIdAndDatePlanIdAsync(datePlanItemId, datePlanId);
                if (datePlanItem == null)
                    throw new Exception("Date plan item not found");

                datePlanItem.IsDeleted = true;
                _unitOfWork.DatePlanItems.Update(datePlanItem);
                _unitOfWork.DatePlans.Update(datePlan);

                return await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<PagedResult<DatePlanItemResponse>> GetAllAsync(int pageNumber, int pageSize, int userId, int datePlanId)
        {
            try
            {
                var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
                if (member == null)
                    throw new Exception("Member not found");

                var couple = await _unitOfWork.CoupleProfiles.GetByMemberIdAsync(member.Id);
                if (couple == null)
                    throw new Exception("Member does not belong to any couples");

                var datePlan = await _unitOfWork.DatePlans.GetByIdAndCoupleIdAsync(datePlanId, couple.id);
                if (datePlan == null)
                    throw new Exception("Date plan not found");

                var (datePlanItems, totalCount) = await _unitOfWork.DatePlanItems.GetPagedAsync(
                        pageNumber,
                        pageSize,
                        dpi => dpi.DatePlanId == datePlanId && dpi.IsDeleted == false,
                        dpi => dpi.OrderBy(dpi => dpi.OrderIndex),
                        dpi => dpi.Include(x => x.VenueLocation)
                    );

                return new PagedResult<DatePlanItemResponse>
                {
                    Items = _mapper.Map<List<DatePlanItemResponse>>(datePlanItems),
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<DatePlanItemResponse> GetDetailDatePlanItemAsync(int userId, int datePlanItemId, int datePlanId)
        {
            try
            {
                var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
                if (member == null)
                    throw new Exception("Member not found");

                var couple = await _unitOfWork.CoupleProfiles.GetByMemberIdAsync(member.Id);
                if (couple == null)
                    throw new Exception("Member does not belong to any couples");

                var datePlan = await _unitOfWork.DatePlans.GetByIdAndCoupleIdAsync(datePlanId, couple.id);
                if (datePlan == null)
                    throw new Exception("Date plan not found");

                var datePlanItem = await _unitOfWork.DatePlanItems.GetByIdAndDatePlanIdAsync(datePlanItemId, datePlanId, includeVenueLocation: true);
                if (datePlanItem == null)
                    throw new Exception("Date plan item not found");

                var response = _mapper.Map<DatePlanItemResponse>(datePlanItem);
                return response;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<DatePlanItemResponse> UpdateItemAsync(int userId, int datePlanId, int datePlanItemId, int version, UpdateDatePlanItemRequest request)
        {
            try
            {
                var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
                if (member == null)
                    throw new Exception("Member not found");

                var couple = await _unitOfWork.CoupleProfiles.GetByMemberIdAsync(member.Id);
                if (couple == null)
                    throw new Exception("Member does not belong to any couples");

                var datePlan = await _unitOfWork.DatePlans.GetByIdAndCoupleIdAsync(datePlanId, couple.id);
                if (datePlan == null)
                    throw new Exception("Date plan not found");

                var datePlanItem = await _unitOfWork.DatePlanItems.GetByIdAndDatePlanIdAsync(datePlanItemId, datePlanId);
                if (datePlanItem == null)
                    throw new Exception("Date plan item not found");

                if (datePlan.Status != DatePlanStatus.DRAFTED.ToString() &&
                    datePlan.Status != DatePlanStatus.PENDING.ToString())
                    throw new Exception("Only date plans with status DRAFTED or PENDING can be updated");

                // Concurrency check
                if (datePlan.Version != version)
                    throw new Exception("The date plan has been modified by another process. Please reload and try again");

                // Validate request
                if (request.StartTime.HasValue)
                    datePlanItem.StartTime = request.StartTime.Value;

                if (request.EndTime.HasValue)
                    datePlanItem.EndTime = request.EndTime.Value;

                if (request.OrderIndex.HasValue)
                    datePlanItem.OrderIndex = request.OrderIndex.Value;

                if (!string.IsNullOrEmpty(request.Note))
                    datePlanItem.Note = request.Note;

                datePlan.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.DatePlanItems.Update(datePlanItem);
                _unitOfWork.DatePlans.Update(datePlan);
                var check = await _unitOfWork.SaveChangesAsync();
                if (check <= 0)
                    throw new Exception("Failed to update date plan item");

                var response = _mapper.Map<DatePlanItemResponse>(datePlanItem);

                return response;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<bool> ReorderDatePlanItemAsync(int userId, int datePlanId, ReorderDatePlanItemsRequest request)
        {
            try
            {
                // Validate input
                var orderedIds = request?.OrderedItemIds ?? new List<int>();
                if (orderedIds.Count == 0)
                    throw new Exception("OrderedItemIds cannot be empty");

                if (orderedIds.Count != orderedIds.Distinct().Count())
                    throw new Exception("Duplicate item ids in request");

                // Auth check
                var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
                if (member == null)
                    throw new Exception("Member not found");

                var couple = await _unitOfWork.CoupleProfiles.GetByMemberIdAsync(member.Id);
                if (couple == null)
                    throw new Exception("Member does not belong to any couples");

                var datePlan = await _unitOfWork.DatePlans.GetByIdAndCoupleIdAsync(datePlanId, couple.id);
                if (datePlan == null)
                    throw new Exception("Date plan not found");

                var datePlanItems = await _unitOfWork.DatePlanItems.GetByDatePlanIdAsync(datePlanId);

                if (orderedIds.Count != datePlanItems.Count())
                    throw new Exception("OrderedItemIds must contain all active items of the plan");

                var activeIdSet = datePlanItems
                    .Select(x => x.Id)
                    .ToHashSet();
                var invalidIds = orderedIds
                    .Where(id => !activeIdSet.Contains(id))
                    .ToList();
                if (invalidIds.Any())
                    throw new Exception($"Some items do not belong to this date plan: {string.Join(", ", invalidIds)}");

                await _unitOfWork.BeginTransactionAsync();

                var newIndexMap = orderedIds
                    .Select((id, idx) => new { id, idx })
                    .ToDictionary(x => x.id, x => x.idx);

                foreach (var item in datePlanItems)
                    item.OrderIndex = newIndexMap[item.Id] + 1;

                _unitOfWork.DatePlanItems.UpdateRange(datePlanItems);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
