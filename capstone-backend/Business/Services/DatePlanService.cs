using AutoMapper;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.DatePlan;
using capstone_backend.Business.DTOs.DatePlanItem;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Jobs.DatePlan;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Extensions.Common;
using Hangfire;

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
                datePlan.Version = 1;

                await _unitOfWork.DatePlans.AddAsync(datePlan);
                return await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<int> DeleteDatePlanAsync(int userId, int datePlanId)
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

                // Check status
                if (datePlan.Status != DatePlanStatus.DRAFTED.ToString() &&
                    datePlan.Status != DatePlanStatus.PENDING.ToString())
                    throw new Exception("Only date plans with status DRAFTED or PENDING can be deleted");

                // Get items
                var datePlanItems = await _unitOfWork.DatePlanItems.GetByDatePlanIdAsync(datePlan.Id);
                datePlanItems = datePlanItems.Select(dpi =>
                {
                    dpi.IsDeleted = true;
                    _unitOfWork.DatePlanItems.Update(dpi);
                    return dpi;
                }).ToList();

                datePlan.IsDeleted = true;
                _unitOfWork.DatePlans.Update(datePlan);
                return await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<(PagedResult<DatePlanResponse>, int totalUpcoming)> GetAllDatePlansByTimeAsync(int pageNumber, int pageSize, int userId, string time)
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

                // Count total upcoming
                var totalUpcoming = await _unitOfWork.DatePlans.CountAsync(dp =>
                    dp.CoupleId == couple.id &&
                    dp.IsDeleted == false &&
                    dp.Status != DatePlanStatus.CANCELLED.ToString() &&
                    ((dp.PlannedEndAt.HasValue && dp.PlannedEndAt >= now) ||
                        (dp.PlannedEndAt == null && dp.PlannedStartAt.HasValue && dp.PlannedStartAt >= now)
                    )
                );

                return (new PagedResult<DatePlanResponse>()
                {
                    Items = _mapper.Map<List<DatePlanResponse>>(items),
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount
                }, totalUpcoming);
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

                var datePlan = await _unitOfWork.DatePlans.GetByIdAndCoupleIdAsync(datePlanId, couple.id, true);
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

        public async Task<DatePlanResponse> UpdateDatePlanAsync(int userId, int datePlanId, int version, UpdateDatePlanRequest request)
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

                // Check status
                if (datePlan.Status != DatePlanStatus.DRAFTED.ToString() &&
                    datePlan.Status != DatePlanStatus.PENDING.ToString())
                    throw new Exception("Only date plans with status DRAFTED or PENDING can be updated");

                // Concurrency check
                if (datePlan.Version != version)
                    throw new Exception("The date plan has been modified by another process. Please reload and try again");

                /// Validate
                if (request.PlannedStartAt.HasValue)
                    request.PlannedStartAt = DateTimeNormalizeUtil.NormalizeToUtc(request.PlannedStartAt.Value);

                if (request.PlannedEndAt.HasValue)
                    request.PlannedEndAt = DateTimeNormalizeUtil.NormalizeToUtc(request.PlannedEndAt.Value);

                var newStart = request.PlannedStartAt ?? datePlan.PlannedStartAt;
                var newEnd = request.PlannedEndAt ?? datePlan.PlannedEndAt;

                if (newStart.HasValue && newEnd.HasValue && newEnd.Value < newStart.Value)
                    throw new Exception("Planned end date must be greater than or equal to planned start date");

                // Apply partial updates
                if (!string.IsNullOrWhiteSpace(request.Title))
                    datePlan.Title = request.Title.Trim();

                if (request.Note != null)
                    datePlan.Note = request.Note;

                if (request.PlannedStartAt.HasValue)
                    datePlan.PlannedStartAt = request.PlannedStartAt.Value;

                if (request.PlannedEndAt.HasValue)
                    datePlan.PlannedEndAt = request.PlannedEndAt.Value;

                if (request.EstimatedBudget.HasValue)
                    datePlan.EstimatedBudget = request.EstimatedBudget.Value;

                datePlan.Version += 1;

                _unitOfWork.DatePlans.Update(datePlan);
                var check = await _unitOfWork.SaveChangesAsync();
                if (check <= 0)
                    throw new Exception("Failed to update date plan");

                var response = _mapper.Map<DatePlanResponse>(datePlan);

                return response;

            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<int> StartDatePlanAsync(int userId, int datePlanId)
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

                // Check status
                if (datePlan.Status != DatePlanStatus.PENDING.ToString())
                    throw new Exception("Only date plans with status PENDING can be started");

                // Start date plan
                datePlan.Status = DatePlanStatus.SCHEDULED.ToString();
                _unitOfWork.DatePlans.Update(datePlan);

                if (datePlan.PlannedStartAt > DateTime.UtcNow && datePlan.PlannedEndAt > DateTime.UtcNow)
                {
                    var jobs = new List<DatePlanJob>();

                    string jobStartId = BackgroundJob.Schedule<IDatePlanWorker>(
                        w => w.StartDatePlanAsync(datePlan.Id),
                        datePlan.PlannedStartAt.Value);

                    // Save job
                    var dateStartPlanJob = new DatePlanJob
                    {
                        DatePlanId = datePlan.Id,
                        JobId = jobStartId,
                        JobType = DatePlanJobType.START.ToString()
                    };
                    jobs.Add(dateStartPlanJob);

                    string jobEndId = BackgroundJob.Schedule<IDatePlanWorker>(
                        w => w.EndDatePlanAsync(datePlan.Id),
                        datePlan.PlannedEndAt.Value);

                    // Save job
                    var dateEndPlanJob = new DatePlanJob
                    {
                        DatePlanId = datePlan.Id,
                        JobId = jobEndId,
                        JobType = DatePlanJobType.END.ToString()
                    };
                    jobs.Add(dateEndPlanJob);

                    string jobReminderId = BackgroundJob.Schedule<IDatePlanWorker>(
                        w => w.SendReminderAsync(datePlan.Id, "DAY"),
                        datePlan.PlannedStartAt.Value.AddMinutes(-8));
                    
                    jobs.Add(new DatePlanJob
                    {
                        DatePlanId = datePlan.Id,
                        JobId = jobReminderId,
                        JobType = DatePlanJobType.REMINDER.ToString()
                    });

                    string jobReminder2Id = BackgroundJob.Schedule<IDatePlanWorker>(
                        w => w.SendReminderAsync(datePlan.Id, "HOUR"),
                        datePlan.PlannedStartAt.Value.AddMinutes(-5));

                    jobs.Add(new DatePlanJob
                    {
                        DatePlanId = datePlan.Id,
                        JobId = jobReminderId,
                        JobType = DatePlanJobType.REMINDER.ToString()
                    });

                    await _unitOfWork.DatePlanJobs.AddRangeAsync(jobs);
                }

                return await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<int> ActionDatePlanAsync(int userId, int datePlanId, DatePlanAction action)
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

                if (action == DatePlanAction.SEND)
                {
                    // Check if whose is organizer
                    if (datePlan.OrganizerMemberId != member.Id)
                        throw new Exception("Only the organizer can send the date plan");
                    datePlan.Status = DatePlanStatus.PENDING.ToString();
                }
                else if (action == DatePlanAction.ACCEPT && datePlan.Status == DatePlanStatus.PENDING.ToString())
                {
                    if (datePlan.OrganizerMemberId == member.Id)
                        throw new Exception("The organizer cannot accept the date plan");
                    datePlan.Status = DatePlanStatus.SCHEDULED.ToString();
                }
                else if (action == DatePlanAction.REJECT)
                {
                    if (datePlan.OrganizerMemberId == member.Id)
                        throw new Exception("The organizer cannot reject the date plan");
                    datePlan.Status = DatePlanStatus.DRAFTED.ToString();
                }
                else
                {
                    throw new Exception("Invalid action or date plan status");
                }

                    _unitOfWork.DatePlans.Update(datePlan);
                return await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }      
    }
}
