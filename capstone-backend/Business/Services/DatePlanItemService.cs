using AutoMapper;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.DatePlanItem;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Extensions.Common;

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


                var items = venues.Select(v =>
                {

                    // Check if item start time is after date plan start time
                    if (v.StartTime.HasValue && v.StartTime.Value < TimeOnly.FromDateTime(TimezoneUtil.ToVietNamTime(datePlan.PlannedStartAt.Value)))
                        throw new Exception("Date plan item start time cannot be before date plan start time");

                    // Check if item end time is after date plan end time
                    if (v.EndTime.HasValue && v.EndTime.Value > TimeOnly.FromDateTime(TimezoneUtil.ToVietNamTime(datePlan.PlannedEndAt.Value)))
                        throw new Exception("Date plan item end time cannot be after date plan end time");

                    var item = _mapper.Map<DatePlanItem>(v);
                    item.DatePlanId = datePlanId;

                    return item;
                }).ToList();

                _unitOfWork.DatePlans.Update(datePlan);
                await _unitOfWork.DatePlanItems.AddRangeAsync(items);
                return await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
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

                var datePlan = await _unitOfWork.DatePlans.GetByIdAndCoupleIdAsync(datePlanId, couple.id, includeVenueLocation: true);
                if (datePlan == null)
                    throw new Exception("Date plan not found");

                var (datePlanItems, totalCount) = await _unitOfWork.DatePlanItems.GetPagedAsync(
                        pageNumber,
                        pageSize,
                        dpi => dpi.DatePlanId == datePlanId && dpi.IsDeleted == false,
                        dpi => dpi.OrderBy(dpi => dpi.OrderIndex)
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
    }
}
