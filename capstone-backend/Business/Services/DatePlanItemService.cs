using AutoMapper;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.DatePlanItem;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;

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

                // Snapshot
                var venues = request.Venues ?? new List<DatePlanItemRequest>();
                if (venues == null || !venues.Any())
                    throw new Exception("Venues cannot be empty");

                var items = venues.Select(v =>
                {
                    var item = _mapper.Map<DatePlanItem>(v);
                    item.DatePlanId = datePlanId;
                    item.Status = DatePlanItemStatus.PLANNED.ToString();

                    return item;
                }).ToList();

                // Update total venues in date plan
                datePlan.TotalCount = (datePlan.TotalCount != 0 ? datePlan.TotalCount : 0) + items.Count;

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
                datePlan.TotalCount = datePlan.TotalCount > 0 ? datePlan.TotalCount - 1 : 0;
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

                var datePlanItem = await _unitOfWork.DatePlanItems.GetByIdAndDatePlanIdAsync(datePlanItemId, datePlanId);
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
