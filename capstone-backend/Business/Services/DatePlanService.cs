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
        private readonly IDatePlanWorker _datePlanWorker;
        private readonly IMapper _mapper;

        public DatePlanService(IUnitOfWork unitOfWork, IMapper mapper, IDatePlanWorker datePlanWorker)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _datePlanWorker = datePlanWorker;
        }

        public async Task<int> CreateDatePlanAsync(int userId, CreateDatePlanRequest request)
        {
            try
            {
                // Check member
                var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
                if (member == null)
                    throw new Exception("Không tìm thấy hồ sơ thành viên");

                // Check couple
                var couple = await _unitOfWork.CoupleProfiles.GetByMemberIdAsync(member.Id);
                if (couple == null)
                    throw new Exception("Thành viên chưa thuộc cặp đôi nào");

                // Nomarlize date
                var plannedStartAtUtc = DateTimeNormalizeUtil.NormalizeToUtc(request.PlannedStartAt);
                var plannedEndAtUtc = DateTimeNormalizeUtil.NormalizeToUtc(request.PlannedEndAt);

                var now = DateTime.UtcNow;

                if (plannedStartAtUtc < now)
                {
                    throw new Exception("Không thể tạo lịch trong quá khứ");
                }

                if (plannedEndAtUtc < plannedStartAtUtc)
                {
                    throw new Exception("Thời gian kết thúc dự kiến không được sớm hơn thời gian bắt đầu dự kiến");
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
                    throw new Exception("Không tìm thấy hồ sơ thành viên");

                var couple = await _unitOfWork.CoupleProfiles.GetByMemberIdAsync(member.Id);
                if (couple == null)
                    throw new Exception("Thành viên chưa thuộc cặp đôi nào");

                var datePlan = await _unitOfWork.DatePlans.GetByIdAndCoupleIdAsync(datePlanId, couple.id);
                if (datePlan == null)
                    throw new Exception("Không tìm thấy lịch trình buổi hẹn");

                // Check status
                if (datePlan.Status != DatePlanStatus.DRAFTED.ToString() &&
                    datePlan.Status != DatePlanStatus.PENDING.ToString())
                    throw new Exception("Chỉ có thể xoá lịch trình buổi hẹn ở trạng thái DRAFTED hoặc PENDING");

                // Get items
                var datePlanItems = await _unitOfWork.DatePlanItems.GetByDatePlanIdAsync(datePlan.Id);
                foreach (var dpi in datePlanItems)
                {
                    dpi.IsDeleted = true;
                    _unitOfWork.DatePlanItems.Update(dpi);
                }

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
                    throw new Exception("Không tìm thấy hồ sơ thành viên");

                var couple = await _unitOfWork.CoupleProfiles.GetByMemberIdAsync(member.Id);
                if (couple == null)
                    throw new Exception("Thành viên chưa thuộc cặp đôi nào");

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
                                      dp.Status == DatePlanStatus.SCHEDULED.ToString() &&
                                      ((dp.PlannedEndAt.HasValue && dp.PlannedEndAt >= now) ||
                                        (dp.PlannedEndAt == null && dp.PlannedStartAt.HasValue && dp.PlannedStartAt >= now)
                                      ),
                                dp => dp.OrderByDescending(dp => dp.CreatedAt)
                            );
                        break;

                    case "PAST":
                        (items, totalCount) = await _unitOfWork.DatePlans.GetPagedAsync(
                                pageNumber,
                                pageSize,
                                dp => dp.CoupleId == couple.id &&
                                      dp.IsDeleted == false &&
                                      dp.Status != DatePlanStatus.CANCELLED.ToString() &&
                                      dp.Status == DatePlanStatus.COMPLETED.ToString() &&
                                      ((dp.PlannedEndAt.HasValue && dp.PlannedEndAt < now) ||
                                        (dp.PlannedEndAt == null && dp.PlannedStartAt.HasValue && dp.PlannedStartAt < now)
                                      ),
                                dp => dp.OrderByDescending(dp => dp.CreatedAt)
                            );
                        break;
                    
                    case "DRAFTED":
                        (items, totalCount) = await _unitOfWork.DatePlans.GetPagedAsync(
                                pageNumber,
                                pageSize,
                                dp => dp.CoupleId == couple.id &&
                                      dp.OrganizerMemberId == member.Id &&
                                      dp.IsDeleted == false &&
                                      dp.Status == DatePlanStatus.DRAFTED.ToString(),
                                dp => dp.OrderByDescending(dp => dp.CreatedAt)
                            );
                        break;

                    case "PENDING":
                        (items, totalCount) = await _unitOfWork.DatePlans.GetPagedAsync(
                                pageNumber,
                                pageSize,
                                dp => dp.CoupleId == couple.id &&
                                      dp.IsDeleted == false &&
                                      dp.Status == DatePlanStatus.PENDING.ToString(),
                                dp => dp.OrderByDescending(dp => dp.CreatedAt)
                            );
                        break;

                    case "ALL":
                        (items, totalCount) = await _unitOfWork.DatePlans.GetPagedAsync(
                            pageNumber,
                            pageSize,
                            dp => dp.CoupleId == couple.id &&
                                  dp.IsDeleted == false &&
                                  dp.Status != DatePlanStatus.CANCELLED.ToString() &&
                                  (
                                      dp.Status != DatePlanStatus.DRAFTED.ToString() ||
                                      dp.OrganizerMemberId == member.Id
                                  ),
                            dp => dp.OrderByDescending(dp => dp.CreatedAt)
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
                    throw new Exception("Không tìm thấy hồ sơ thành viên");

                var couple = await _unitOfWork.CoupleProfiles.GetByMemberIdAsync(member.Id);
                if (couple == null)
                    throw new Exception("Thành viên chưa thuộc cặp đôi nào");

                var datePlan = await _unitOfWork.DatePlans.GetByIdAndCoupleIdAsync(datePlanId, couple.id, true, true);
                if (datePlan == null)
                    throw new Exception("Không tìm thấy lịch trình buổi hẹn");

                if (datePlan.Status == DatePlanStatus.DRAFTED.ToString() && datePlan.OrganizerMemberId != member.Id)
                    throw new Exception("Chỉ người tổ chức mới có quyền xem lịch trình buổi hẹn ở trạng thái DRAFTED");

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
                    throw new Exception("Không tìm thấy hồ sơ thành viên");

                var couple = await _unitOfWork.CoupleProfiles.GetByMemberIdAsync(member.Id);
                if (couple == null)
                    throw new Exception("Thành viên chưa thuộc cặp đôi nào");

                var datePlan = await _unitOfWork.DatePlans.GetByIdAndCoupleIdAsync(datePlanId, couple.id);
                if (datePlan == null)
                    throw new Exception("Không tìm thấy lịch trình buổi hẹn");

                // Check status
                if (datePlan.Status != DatePlanStatus.DRAFTED.ToString() &&
                    datePlan.Status != DatePlanStatus.PENDING.ToString())
                    throw new Exception("Chỉ có thể cập nhật lịch trình buổi hẹn ở trạng thái DRAFTED hoặc PENDING");

                // Concurrency check
                if (datePlan.Version != version)
                    throw new Exception("Lịch trình đã được chỉnh sửa bởi người khác. Vui lòng tải lại và thử lại");

                /// Validate
                if (request.PlannedStartAt.HasValue)
                    request.PlannedStartAt = DateTimeNormalizeUtil.NormalizeToUtc(request.PlannedStartAt.Value);

                if (request.PlannedEndAt.HasValue)
                    request.PlannedEndAt = DateTimeNormalizeUtil.NormalizeToUtc(request.PlannedEndAt.Value);

                var newStart = request.PlannedStartAt ?? datePlan.PlannedStartAt;
                var newEnd = request.PlannedEndAt ?? datePlan.PlannedEndAt;

                if (newStart.HasValue && newEnd.HasValue && newEnd.Value < newStart.Value)
                    throw new Exception("Thời gian kết thúc dự kiến không được sớm hơn thời gian bắt đầu dự kiến");

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
                    throw new Exception("Cập nhật lịch trình thất bại");

                var response = _mapper.Map<DatePlanResponse>(datePlan);

                return response;

            }
            catch (Exception)
            {

                throw;
            }
        }

        private async Task StartDatePlanAsync(DatePlan datePlan)
        {
            try
            {
                var now = DateTime.UtcNow;
                var jobs = new List<DatePlanJob>();
                if (datePlan.PlannedStartAt > now)
                {


                    string jobStartId = BackgroundJob.Schedule<IDatePlanWorker>(
                        w => w.StartDatePlanAsync(datePlan.Id),
                        datePlan.PlannedStartAt.Value);

                    // Save job
                    jobs.Add(new DatePlanJob
                    {
                        DatePlanId = datePlan.Id,
                        JobId = jobStartId,
                        JobType = DatePlanJobType.START.ToString()
                    });
                }

                if (datePlan.PlannedEndAt > now)
                {
                    string jobEndId = BackgroundJob.Schedule<IDatePlanWorker>(
                        w => w.EndDatePlanAsync(datePlan.Id),
                        datePlan.PlannedEndAt.Value);

                    // Save job
                    jobs.Add(new DatePlanJob
                    {
                        DatePlanId = datePlan.Id,
                        JobId = jobEndId,
                        JobType = DatePlanJobType.END.ToString()
                    });
                }

                if (datePlan.PlannedStartAt.HasValue && datePlan.PlannedStartAt.Value.AddDays(-1) > now)
                {
                    string jobReminder1Id = BackgroundJob.Schedule<IDatePlanWorker>(
                        w => w.SendReminderAsync(datePlan.Id, "DAY"),
                        datePlan.PlannedStartAt.Value.AddDays(-1));
                    jobs.Add(new DatePlanJob
                    {
                        DatePlanId = datePlan.Id,
                        JobId = jobReminder1Id,
                        JobType = DatePlanJobType.REMINDER.ToString()
                    });
                }
            
                if (datePlan.PlannedStartAt.HasValue && datePlan.PlannedStartAt.Value.AddHours(-1) > now)
                {
                    string jobReminder2Id = BackgroundJob.Schedule<IDatePlanWorker>(
                        w => w.SendReminderAsync(datePlan.Id, "HOUR"),
                        datePlan.PlannedStartAt.Value.AddHours(-1));
                    jobs.Add(new DatePlanJob
                    {
                        DatePlanId = datePlan.Id,
                        JobId = jobReminder2Id,
                        JobType = DatePlanJobType.REMINDER.ToString()
                    });
                }

                if (jobs.Count > 0)
                    await _unitOfWork.DatePlanJobs.AddRangeAsync(jobs);
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
                    throw new Exception("Không tìm thấy hồ sơ thành viên");

                var couple = await _unitOfWork.CoupleProfiles.GetByMemberIdAsync(member.Id);
                if (couple == null)
                    throw new Exception("Thành viên chưa thuộc cặp đôi nào");

                var datePlan = await _unitOfWork.DatePlans.GetByIdAndCoupleIdAsync(datePlanId, couple.id);
                if (datePlan == null)
                    throw new Exception("Không tìm thấy lịch trình buổi hẹn");

                if (action == DatePlanAction.SEND)
                {
                    // Check if whose is organizer
                    if (datePlan.OrganizerMemberId != member.Id)
                        throw new Exception("Chỉ người tổ chức mới có quyền gửi lịch trình buổi hẹn");
                    datePlan.Status = DatePlanStatus.PENDING.ToString();
                }
                else if (action == DatePlanAction.ACCEPT && datePlan.Status == DatePlanStatus.PENDING.ToString())
                {
                    if (datePlan.OrganizerMemberId == member.Id)
                        throw new Exception("Người tổ chức không thể chấp nhận lịch trình buổi hẹn");
                    datePlan.Status = DatePlanStatus.SCHEDULED.ToString();

                    // Start date plan jobs
                    await StartDatePlanAsync(datePlan);
                }
                else if (action == DatePlanAction.REJECT && datePlan.Status == DatePlanStatus.PENDING.ToString())
                {
                    if (datePlan.OrganizerMemberId == member.Id)
                        throw new Exception("Người tổ chức không thể từ chối lịch trình buổi hẹn");
                    datePlan.Status = DatePlanStatus.DRAFTED.ToString();
                }
                else if (action == DatePlanAction.CANCEL && (datePlan.Status == DatePlanStatus.SCHEDULED.ToString() || datePlan.Status == DatePlanStatus.IN_PROGRESS.ToString()))
                {
                    if (datePlan.OrganizerMemberId != member.Id)
                        throw new Exception("Chỉ người tổ chức mới có quyền huỷ trình buổi hẹn");
                    datePlan.Status = DatePlanStatus.CANCELLED.ToString();

                    // Remove jobs
                    await _datePlanWorker.CleanupAllJobsAsync(datePlan.Id);
                }
                else if (action == DatePlanAction.COMPLETE && datePlan.Status == DatePlanStatus.IN_PROGRESS.ToString())
                {
                    if (datePlan.OrganizerMemberId != member.Id)
                        throw new Exception("Chỉ người tổ chức mới có quyền hoàn thành lịch trình buổi hẹn");
                    datePlan.Status = DatePlanStatus.COMPLETED.ToString();
                    datePlan.CompletedAt = DateTime.UtcNow;
                    // Remove jobs
                    await _datePlanWorker.CleanupAllJobsAsync(datePlan.Id);
                }
                else
                {
                    throw new Exception("Thao tác không hợp lệ hoặc trạng thái lịch trình buổi hẹn không phù hợp");
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
