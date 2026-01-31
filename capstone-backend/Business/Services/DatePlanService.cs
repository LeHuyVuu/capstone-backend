using AutoMapper;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.DatePlan;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Extensions.Common;

namespace capstone_backend.Business.Services
{
    public class DatePlanService : IDatePlanService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public DatePlanService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<int> CreateDatePlanAsync(int userId, CreateDatePlanRequest request)
        {
            try
            {
                // Check member
                var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
                if (member == null)
                    throw new Exception("Member not found");

                // Check couple
                var couple = await _unitOfWork.CoupleProfiles.GetByMemberIdAsync(member.Id);
                if (couple == null)
                    throw new Exception("Member does not belong to any couples");

                // Nomarlize date
                var plannedStartAtUtc = DateTimeNormalizeUtil.NormalizeToUtc(request.PlannedStartAt);
                var plannedEndAtUtc = DateTimeNormalizeUtil.NormalizeToUtc(request.PlannedEndAt);

                if (plannedEndAtUtc < plannedStartAtUtc)
                {
                    throw new Exception("Planned end date must be greater than or equal to planned start date");
                }

                // Create date plan
                var datePlan = _mapper.Map<DatePlan>(request);
                datePlan.CoupleId = couple.id;
                datePlan.OrganizerMemberId = member.Id;
                datePlan.Status = DatePlanStatus.DRAFTED.ToString();
                datePlan.PlannedStartAt = plannedStartAtUtc;
                datePlan.PlannedEndAt = plannedEndAtUtc;

                await _unitOfWork.DatePlans.AddAsync(datePlan);
                return await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<PagedResult<DatePlanResponse>> GetAllDatePlansByTimeAsync(int pageNumber, int pageSize, int userId, string time)
        {
            try
            {
                var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
                if (member == null)
                    throw new Exception("Member not found");

                var couple = await _unitOfWork.CoupleProfiles.GetByMemberIdAsync(member.Id);
                if (couple == null)
                    throw new Exception("Member does not belong to any couples");

                var now = DateTime.UtcNow;
                IEnumerable<DatePlan> items = Enumerable.Empty<DatePlan>();
                var totalCount = 0;

                switch (time)
                {
                    case "UPCOMING":
                        (items, totalCount) = await _unitOfWork.DatePlans.GetPagedAsync(
                                pageNumber,
                                pageSize,
                                dp => dp.CoupleId == couple.id &&
                                      dp.IsDeleted == false &&
                                      dp.Status != DatePlanStatus.CANCELLED.ToString() &&
                                      ((dp.PlannedEndAt.HasValue && dp.PlannedEndAt >= now) ||
                                        (dp.PlannedEndAt == null && dp.PlannedStartAt.HasValue && dp.PlannedStartAt >= now)
                                      ),
                                dp => dp.OrderBy(dp => dp.PlannedStartAt ?? dp.CreatedAt)
                            );
                        break;

                    case "PAST":
                        (items, totalCount) = await _unitOfWork.DatePlans.GetPagedAsync(
                                pageNumber,
                                pageSize,
                                dp => dp.CoupleId == couple.id &&
                                      dp.IsDeleted == false &&
                                      dp.Status != DatePlanStatus.CANCELLED.ToString() &&
                                      ((dp.PlannedEndAt.HasValue && dp.PlannedEndAt < now) ||
                                        (dp.PlannedEndAt == null && dp.PlannedStartAt.HasValue && dp.PlannedStartAt < now)
                                      ),
                                dp => dp.OrderBy(dp => dp.PlannedStartAt ?? dp.CreatedAt)
                            );
                        break;

                    case "ALL":
                        (items, totalCount) = await _unitOfWork.DatePlans.GetPagedAsync(
                                pageNumber,
                                pageSize,
                                dp => dp.CoupleId == couple.id &&
                                      dp.IsDeleted == false &&
                                      dp.Status != DatePlanStatus.CANCELLED.ToString(),
                                dp => dp.OrderBy(dp => dp.PlannedStartAt ?? dp.CreatedAt)
                            );
                        break;

                    default:
                        break;
                }

                return new PagedResult<DatePlanResponse>()
                {
                    Items = _mapper.Map<List<DatePlanResponse>>(items),
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount
                };
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<DatePlanDetailResponse> GetByIdAsync(int datePlanId, int userId)
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

                var response = _mapper.Map<DatePlanDetailResponse>(datePlan);

                return response;
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
