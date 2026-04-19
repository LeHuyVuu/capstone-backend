using AutoMapper;
using capstone_backend.Business.Common;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.DatePlan;
using capstone_backend.Business.DTOs.DatePlanItem;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Jobs.DatePlan;
using capstone_backend.Business.Jobs.Notification;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Extensions.Common;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;
using System.ClientModel;
using static capstone_backend.Business.Services.VenueLocationService;

namespace capstone_backend.Business.Services
{
    public class DatePlanService : IDatePlanService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDatePlanWorker _datePlanWorker;
        private readonly IMapper _mapper;
        private readonly Lazy<ChatClient> _chatClientLazy;
        private readonly ISystemConfigService _systemConfigService;

        public DatePlanService(IUnitOfWork unitOfWork, IMapper mapper, IDatePlanWorker datePlanWorker, ISystemConfigService systemConfigService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _datePlanWorker = datePlanWorker;

            _chatClientLazy = new Lazy<ChatClient>(() =>
            {
                var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                var modelName = Environment.GetEnvironmentVariable("MODEL_NAME") ?? "gpt-4o-mini";

                if (string.IsNullOrWhiteSpace(apiKey))
                    throw new InvalidOperationException("Thiếu OpenAI API Key!");

                return new ChatClient(model: modelName, apiKey: apiKey);
            });
            _systemConfigService = systemConfigService;
        }

        public async Task<int> CreateDatePlanAsync(int userId, CreateDatePlanRequest request)
        {
            // Check member
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Không tìm thấy hồ sơ thành viên");

            // Check couple
            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
            if (couple == null)
                throw new Exception("Thành viên chưa thuộc cặp đôi nào");

            // Nomarlize date
            var plannedStartAtUtc = DateTimeNormalizeUtil.NormalizeToUtc(request.PlannedStartAt);
            var plannedEndAtUtc = DateTimeNormalizeUtil.NormalizeToUtc(request.PlannedEndAt);

            ValidateDatePlanTime(plannedStartAtUtc, plannedEndAtUtc, request.DurationMode);
            await ValidateDatePlanOverlapAsync(couple.id, plannedStartAtUtc, plannedEndAtUtc);

            // Create date plan
            var datePlan = _mapper.Map<DatePlan>(request);
            datePlan.CoupleId = couple.id;
            datePlan.OrganizerMemberId = member.Id;
            datePlan.Status = DatePlanStatus.DRAFTED.ToString();
            datePlan.PlannedStartAt = plannedStartAtUtc;
            datePlan.PlannedEndAt = plannedEndAtUtc;
            datePlan.Version = 1;

            datePlan.DurationMode = request.DurationMode.ToString();

            await _unitOfWork.DatePlans.AddAsync(datePlan);
            await _unitOfWork.SaveChangesAsync();
            return datePlan.Id;
        }

        private static void ValidateDatePlanTime(
            DateTime startUtc,
            DateTime endUtc,
            DatePlanDurationMode mode)
        {
            var now = DateTime.UtcNow;

            if (startUtc < now)
                throw new Exception("Không thể tạo lịch trong quá khứ");

            if (endUtc < startUtc)
                throw new Exception("Thời gian kết thúc dự kiến không được sớm hơn thời gian bắt đầu dự kiến");

            if ((endUtc - startUtc) < TimeSpan.FromHours(1))
                throw new Exception("Thời gian bắt đầu và kết thúc phải cách nhau ít nhất 1 giờ");

            var startVn = TimezoneUtil.ToVietNamTime(startUtc);
            var endVn = TimezoneUtil.ToVietNamTime(endUtc);

            switch (mode)
            {
                case DatePlanDurationMode.SAME_DAY:
                    if (startVn.Date != endVn.Date)
                        throw new Exception("Lịch trình mặc định chỉ được tạo trong cùng một ngày");
                    break;

                case DatePlanDurationMode.WITHIN_24_HOURS:
                    if ((endUtc - startUtc) > TimeSpan.FromHours(24))
                        throw new Exception("Lịch trình dạng 24 giờ không được vượt quá 24 giờ");
                    break;

                default:
                    throw new Exception("Kiểu thời lượng lịch trình không hợp lệ");
            }
        }

        public async Task<int> DeleteDatePlanAsync(int userId, int datePlanId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Không tìm thấy hồ sơ thành viên");

            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
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

        public async Task<(PagedResult<DatePlanResponse>, int totalUpcoming)> GetAllDatePlansByTimeAsync(int pageNumber, int pageSize, int userId, string status)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Không tìm thấy hồ sơ thành viên");

            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
            if (couple == null)
                throw new Exception("Thành viên chưa thuộc cặp đôi nào");

            var now = DateTime.UtcNow;
            IEnumerable<DatePlan> items = Enumerable.Empty<DatePlan>();
            var totalCount = 0;

            switch (status)
            {
                case "UPCOMING":
                    (items, totalCount) = await _unitOfWork.DatePlans.GetPagedAsync(
                            pageNumber,
                            pageSize,
                            dp => dp.CoupleId == couple.id &&
                                  dp.IsDeleted == false &&
                                  dp.Status == DatePlanStatus.SCHEDULED.ToString() &&
                                  ((dp.PlannedEndAt.HasValue && dp.PlannedEndAt >= now) ||
                                    (dp.PlannedEndAt == null && dp.PlannedStartAt.HasValue && dp.PlannedStartAt >= now)
                                  ),
                            dp => dp.OrderByDescending(dp => dp.CreatedAt)
                        );
                    break;

                case "COMPLETED":
                    (items, totalCount) = await _unitOfWork.DatePlans.GetPagedAsync(
                            pageNumber,
                            pageSize,
                            dp => dp.CoupleId == couple.id &&
                                  dp.IsDeleted == false &&
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

                case "CANCELLED":
                    (items, totalCount) = await _unitOfWork.DatePlans.GetPagedAsync(
                        pageNumber,
                        pageSize,
                        dp => dp.CoupleId == couple.id &&
                              dp.IsDeleted == false &&
                              dp.Status == DatePlanStatus.CANCELLED.ToString(),
                        dp => dp.OrderByDescending(dp => dp.CreatedAt)
                    );
                    break;

                case "IN_PROGRESS":
                    (items, totalCount) = await _unitOfWork.DatePlans.GetPagedAsync(
                        pageNumber,
                        pageSize,
                        dp => dp.CoupleId == couple.id &&
                              dp.IsDeleted == false &&
                              dp.Status == DatePlanStatus.IN_PROGRESS.ToString(),
                        dp => dp.OrderByDescending(dp => dp.CreatedAt)
                    );
                    break;

                case "ALL":
                    (items, totalCount) = await _unitOfWork.DatePlans.GetPagedAsync(
                        pageNumber,
                        pageSize,
                        dp => dp.CoupleId == couple.id &&
                              dp.IsDeleted == false &&
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
                dp.Status == DatePlanStatus.SCHEDULED.ToString() &&
                ((dp.PlannedEndAt.HasValue && dp.PlannedEndAt >= now) ||
                    (dp.PlannedEndAt == null && dp.PlannedStartAt.HasValue && dp.PlannedStartAt >= now)
                )
            );

            var responses = _mapper.Map<List<DatePlanResponse>>(items);
            foreach (var item in responses)
            {
                item.IsCreator = item.OrganizerMemberId == member.Id;
            }

            return (new PagedResult<DatePlanResponse>()
            {
                Items = responses,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            }, totalUpcoming);
        }

        public async Task<DatePlanDetailResponse> GetByIdAsync(int datePlanId, int userId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Không tìm thấy hồ sơ thành viên");

            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
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

        public async Task<DatePlanResponse> UpdateDatePlanAsync(int userId, int datePlanId, int version, UpdateDatePlanRequest request)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Không tìm thấy hồ sơ thành viên");

            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
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

            var newMode = request.DurationMode ?? Enum.Parse<DatePlanDurationMode>(datePlan.DurationMode);

            var newStart = request.PlannedStartAt ?? datePlan.PlannedStartAt;
            var newEnd = request.PlannedEndAt ?? datePlan.PlannedEndAt;

            if (!newStart.HasValue || !newEnd.HasValue)
                throw new Exception("Lịch trình chưa có đầy đủ thời gian bắt đầu và kết thúc");

            ValidateDatePlanTime(newStart.Value, newEnd.Value, newMode);
            await ValidateDatePlanOverlapAsync(couple.id, newStart.Value, newEnd.Value, datePlan.Id);

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

            datePlan.DurationMode = newMode.ToString();

            _unitOfWork.DatePlans.Update(datePlan);
            var check = await _unitOfWork.SaveChangesAsync();
            if (check <= 0)
                throw new Exception("Cập nhật lịch trình thất bại");

            var response = _mapper.Map<DatePlanResponse>(datePlan);

            return response;
        }

        private async Task StartDatePlanAsync(DatePlan datePlan)
        {
            var now = DateTime.UtcNow;
            var (reminder1Sec, reminder2Sec) = await GetDatePlanReminderSecondsAsync();
            var jobs = new List<DatePlanJob>();

            if (datePlan.PlannedStartAt.HasValue)
            {
                var reminder1At = datePlan.PlannedStartAt.Value.AddSeconds(-reminder1Sec);
                if (reminder1At > now)
                {
                    string jobReminder1Id = BackgroundJob.Schedule<IDatePlanWorker>(
                        w => w.SendReminderAsync(datePlan.Id, "PRIMARY"),
                        reminder1At);

                    jobs.Add(new DatePlanJob
                    {
                        DatePlanId = datePlan.Id,
                        JobId = jobReminder1Id,
                        JobType = DatePlanJobType.REMINDER.ToString()
                    });
                }

                var reminder2At = datePlan.PlannedStartAt.Value.AddSeconds(-reminder2Sec);
                if (reminder2At > now)
                {
                    string jobReminder2Id = BackgroundJob.Schedule<IDatePlanWorker>(
                        w => w.SendReminderAsync(datePlan.Id, "SECONDARY"),
                        reminder2At);

                    jobs.Add(new DatePlanJob
                    {
                        DatePlanId = datePlan.Id,
                        JobId = jobReminder2Id,
                        JobType = DatePlanJobType.REMINDER.ToString()
                    });
                }
            }

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

            if (jobs.Count > 0)
                await _unitOfWork.DatePlanJobs.AddRangeAsync(jobs);
        }

        private async Task<(int reminder1Sec, int reminder2Sec)> GetDatePlanReminderSecondsAsync()
        {
            const int fallback1 = 3600; // mặc định reminder 1: 1h trước
            const int fallback2 = 900;  // mặc định reminder 2: 15p trước

            int r1 = fallback1;
            int r2 = fallback2;

            try
            {
                r1 = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.DATEPLAN_REMINDER_1_BEFORE_SECONDS.ToString());
            }
            catch { }

            try
            {
                r2 = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.DATEPLAN_REMINDER_2_BEFORE_SECONDS.ToString());
            }
            catch { }

            r1 = Math.Clamp(r1, 5, 43200);
            r2 = Math.Clamp(r2, 5, 43200);

            // reminder1 phải sớm hơn reminder2
            if (r1 <= r2)
            {
                var tmp = r1;
                r1 = r2;
                r2 = tmp;
            }

            return (r1, r2);
        }

        public async Task<int> ActionDatePlanAsync(int userId, int datePlanId, DatePlanAction action)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Không tìm thấy hồ sơ thành viên");

            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
            if (couple == null)
                throw new Exception("Thành viên chưa thuộc cặp đôi nào");

            var datePlan = await _unitOfWork.DatePlans.GetByIdAndCoupleIdAsync(datePlanId, couple.id);
            if (datePlan == null)
                throw new Exception("Không tìm thấy lịch trình buổi hẹn");

            var notifyRejected = false;
            var notifyCancelled = false;
            var notifyCompleted = false;

            if (action == DatePlanAction.SEND)
            {
                if (!datePlan.PlannedStartAt.HasValue || !datePlan.PlannedEndAt.HasValue)
                    throw new Exception("Lịch trình chưa có đầy đủ thời gian bắt đầu và kết thúc");

                var mode = Enum.Parse<DatePlanDurationMode>(datePlan.DurationMode);
                ValidateDatePlanTime(datePlan.PlannedStartAt.Value, datePlan.PlannedEndAt.Value, mode);

                // Check if whose is organizer
                if (datePlan.OrganizerMemberId != member.Id)
                    throw new Exception("Chỉ người tổ chức mới có quyền gửi lịch trình buổi hẹn");
                datePlan.Status = DatePlanStatus.PENDING.ToString();
            }
            else if (action == DatePlanAction.ACCEPT && datePlan.Status == DatePlanStatus.PENDING.ToString())
            {
                if (!datePlan.PlannedStartAt.HasValue || !datePlan.PlannedEndAt.HasValue)
                    throw new Exception("Lịch trình chưa có đầy đủ thời gian bắt đầu và kết thúc");

                var mode = Enum.Parse<DatePlanDurationMode>(datePlan.DurationMode);
                ValidateDatePlanTime(datePlan.PlannedStartAt.Value, datePlan.PlannedEndAt.Value, mode);

                if (datePlan.OrganizerMemberId == member.Id)
                    throw new Exception("Người tổ chức không thể chấp nhận lịch trình buổi hẹn");
                datePlan.Status = DatePlanStatus.SCHEDULED.ToString();

                // Notify sender date plan
                var notification = new Notification
                {
                    UserId = datePlan.OrganizerMember.UserId,
                    Title = NotificationTemplate.DatePlan.TitleAccepted,
                    Message = NotificationTemplate.DatePlan.GetAcceptedBody(datePlan.Title),
                    Type = NotificationType.PAIRING.ToString(),
                    ReferenceId = datePlan.Id,
                    ReferenceType = ReferenceType.DATE_PLAN.ToString(),
                    IsRead = false,
                };

                await _unitOfWork.Notifications.AddAsync(notification);
                await _unitOfWork.SaveChangesAsync();

                BackgroundJob.Enqueue<INotificationWorker>(s => s.SendPushNotificationAsync(notification.Id));

                // Start date plan jobs
                await StartDatePlanAsync(datePlan);
            }
            else if (action == DatePlanAction.REJECT && datePlan.Status == DatePlanStatus.PENDING.ToString())
            {
                if (datePlan.OrganizerMemberId == member.Id)
                    throw new Exception("Người tổ chức không thể từ chối lịch trình buổi hẹn");
                datePlan.Status = DatePlanStatus.DRAFTED.ToString();
                notifyRejected = true;
            }
            else if (action == DatePlanAction.CANCEL && (datePlan.Status == DatePlanStatus.SCHEDULED.ToString() || datePlan.Status == DatePlanStatus.IN_PROGRESS.ToString()))
            {
                if (datePlan.OrganizerMemberId != member.Id)
                    throw new Exception("Chỉ người tổ chức mới có quyền huỷ trình buổi hẹn");
                datePlan.Status = DatePlanStatus.CANCELLED.ToString();

                // Remove jobs
                await _datePlanWorker.CleanupAllJobsAsync(datePlan.Id);

                notifyCancelled = true;
            }
            else if (action == DatePlanAction.COMPLETE && datePlan.Status == DatePlanStatus.IN_PROGRESS.ToString())
            {
                if (datePlan.OrganizerMemberId != member.Id)
                    throw new Exception("Chỉ người tổ chức mới có quyền hoàn thành lịch trình buổi hẹn");
                datePlan.Status = DatePlanStatus.COMPLETED.ToString();
                datePlan.CompletedAt = DateTime.UtcNow;
                // Remove jobs
                await _datePlanWorker.CleanupAllJobsAsync(datePlan.Id);

                notifyCompleted = true;
            }
            else
            {
                throw new Exception("Thao tác không hợp lệ hoặc trạng thái lịch trình buổi hẹn không phù hợp");
            }

            _unitOfWork.DatePlans.Update(datePlan);
            var result = await _unitOfWork.SaveChangesAsync();

            if (result > 0)
            {
                if (notifyRejected)
                    await _datePlanWorker.SendRejectedNotificationAsync(datePlan.Id);

                if (notifyCancelled)
                    await _datePlanWorker.SendCancelledNotificationAsync(datePlan.Id);

                if (notifyCompleted)
                    await _datePlanWorker.SendCompletedNotificationAsync(datePlan.Id);
            }

            return result;
        }

        private async Task ValidateDatePlanOverlapAsync(
            int coupleId,
            DateTime startUtc,
            DateTime endUtc,
            int? excludeDatePlanId = null)
        {
            var overlapped = await _unitOfWork.DatePlans.HasOverlappingAsync(coupleId, startUtc, endUtc, excludeDatePlanId);

            if (overlapped)
                throw new Exception("Đã tồn tại lịch trình khác bị trùng khoảng thời gian");
        }

        public async Task<DatePlanCalendar30DaysResponse> GetDatePlansIn30DaysAsync(int userId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
            if (couple == null)
                throw new Exception("Thành viên chưa thuộc cặp đôi nào");

            var vnNow = TimezoneUtil.ToVietNamTime(DateTime.UtcNow);
            var startDate = DateOnly.FromDateTime(vnNow.Date.AddDays(-3));
            var endDate = startDate.AddDays(29);

            var startUtc = DateTimeNormalizeUtil.NormalizeToUtc(startDate.ToDateTime(TimeOnly.MinValue));
            var endUtcExclusive = DateTimeNormalizeUtil.NormalizeToUtc(endDate.AddDays(1).ToDateTime(TimeOnly.MinValue));

            var datePlans = await _unitOfWork.DatePlans.GetAsync(dp =>
                dp.IsDeleted == false && (dp.Status == DatePlanStatus.SCHEDULED.ToString() || dp.Status == DatePlanStatus.IN_PROGRESS.ToString()) &&
                dp.CoupleId == couple.id &&
                dp.PlannedStartAt != null &&
                dp.PlannedStartAt >= startUtc &&
                dp.PlannedStartAt < endUtcExclusive
            );

            var lookup = datePlans
                .GroupBy(x => DateOnly.FromDateTime(TimezoneUtil.ToVietNamTime(x.PlannedStartAt!.Value)))
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(x => x.PlannedStartAt)
                        .Select(x => x.Id)
                        .ToList()
                );

            var days = new List<DatePlanCalendarDayItemResponse>();

            for (int i = 0; i < 30; i++)
            {
                var date = startDate.AddDays(i);
                lookup.TryGetValue(date, out var ids);

                days.Add(new DatePlanCalendarDayItemResponse
                {
                    Date = date,
                    HasDatePlan = ids != null && ids.Count > 0,
                    DatePlanIds = ids ?? new List<int>()
                });
            }

            return new DatePlanCalendar30DaysResponse
            {
                StartDay = startDate,
                EndDay = endDate,
                Days = days
            };
        }

        public async Task<object> GetAISuggestionAsync(int userId, bool previewOnly, DatePlanAISuggestionRequest request)
        {
            var chatClient = _chatClientLazy.Value;

            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Không tìm thấy hồ sơ thành viên");

            var personalityMember = await _unitOfWork.PersonalityTests.GetCurrentPersonalityAsync(member.Id);

            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleIncludePersonalityAndMoodByMemberIdAsync(member.Id);
            if (couple == null)
                throw new Exception("Thành viên chưa thuộc cặp đôi nào");

            // Phase 1: Extract user query
            var activeCategories = await _unitOfWork.Context.Set<Category>()
                .Where(c => c.IsActive && !c.IsDeleted)
                .Select(c => c.Name)
                .ToListAsync();
            var categoryStringList = string.Join(", ", activeCategories);
            var phase1SystemPrompt = BuildPhase1SystemPrompt(categoryStringList);
            var phase1UserPrompt = string.IsNullOrWhiteSpace(request.Query)
                ? "Không có yêu cầu cụ thể"
                : $"User query: '{request.Query.Trim()}'";

            var options = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
                Temperature = 0.2f
            };

            var messages = new List<ChatMessage>()
            {
                new SystemChatMessage(phase1SystemPrompt),
                new UserChatMessage(phase1UserPrompt)
            };

            AiExtractedIntentResponse? extractedIntent;

            try
            {
                // Call OpenAI API
                ClientResult<ChatCompletion> response = await chatClient.CompleteChatAsync(messages, options);

                var jsonString = response.Value.Content[0].Text;

                extractedIntent = JsonConverterUtil.DeserializeOrDefault<AiExtractedIntentResponse>(jsonString);

                if (extractedIntent == null)
                    throw new Exception("AI không trả về đúng định dạng JSON.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi phân tích yêu cầu bằng AI: {ex.Message}");
            }

            // Phase 2: Filter
            var plannedStartAtUtc = DateTimeNormalizeUtil.NormalizeToUtc(request.PlannedStartAt);
            var plannedEndAtUtc = DateTimeNormalizeUtil.NormalizeToUtc(request.PlannedEndAt);

            var plannedStartAtVn = TimezoneUtil.ToVietNamTime(plannedStartAtUtc);
            var plannedEndAtVn = TimezoneUtil.ToVietNamTime(plannedEndAtUtc);

            var targetVenueCount = GetTargetVenueCount(plannedStartAtVn, plannedEndAtVn);

            var startDayOfWeek = (int)request.PlannedStartAt.DayOfWeek + 1;
            var startTimeSpan = TimeOnly.FromDateTime(plannedStartAtVn).ToTimeSpan();
            var endTimeSpan = TimeOnly.FromDateTime(plannedEndAtVn).ToTimeSpan();

            var venueQuery = _unitOfWork.VenueLocations.BuildAiCandidatesQuery(
                request.EstimatedBudget,
                activeCategories,
                startDayOfWeek,
                startTimeSpan,
                endTimeSpan,
                request.Latitude.HasValue ? request.Latitude.Value : null,
                request.Longitude.HasValue ? request.Longitude.Value : null,
                20.0
            );

            var rawVenues = await venueQuery
                .OrderByDescending(v => v.AverageRating)
                .ThenByDescending(v => v.ReviewCount)
                .Take(50)
                .Select(v => new
                {
                    v.Id,
                    v.Name,
                    v.Description,
                    v.Address,
                    v.AverageRating,
                    v.Latitude,
                    v.Longitude,
                    v.CoverImage,
                    Categories = _unitOfWork.Context.Set<VenueLocationCategory>()
                        .Where(vlc => vlc.VenueLocationId == v.Id && vlc.IsDeleted == false)
                        .Select(vlc => vlc.Category.Name)
                        .ToList()
                }).ToListAsync();

            // filter with lat/long
            if (request.Latitude.HasValue && request.Longitude.HasValue)
            {
                var reqLat = request.Latitude.Value;
                var reqLon = request.Longitude.Value;
                var maxDistanceKm = 20.0;

                rawVenues = rawVenues
                    .Where(v => v.Latitude.HasValue && v.Longitude.HasValue)
                    .Where(v => GeoCalculator.CalculateDistance(
                        reqLat,
                        reqLon,
                        v.Latitude.Value,
                        v.Longitude.Value) <= maxDistanceKm)
                    .ToList();
            }

            var aiCandidates = rawVenues.Select(v => new VenueCandidateDto
            {
                Id = v.Id,
                Name = v.Name,
                Description = v.Description,
                Address = v.Address,
                Rating = v.AverageRating.Value,
                Categories = v.Categories
            }).ToList();

            // Phase 3: Request to AI
            // Build context

            // Random
            var rng = new Random(Guid.NewGuid().GetHashCode());
            aiCandidates = aiCandidates
                .OrderBy(_ => rng.Next())
                .Take(20)
                .ToList();

            var aiPromptRequest = new AIRecommendationDatePlanRequest
            {
                RequestContext = new RequestContextDto
                {
                    EstimatedBudget = request.EstimatedBudget,
                    PlannedStartAt = TimezoneUtil.ToVietNamTime(request.PlannedStartAt).ToString("HH:mm"),
                    PlannedEndAt = TimezoneUtil.ToVietNamTime(request.PlannedEndAt).ToString("HH:mm"),
                    RawQuery = request.Query,
                    UserIntent = extractedIntent,
                    Address = request.Address
                },
                CoupleContext = new CoupleContextDto
                {
                    Ages = member.DateOfBirth.HasValue ? new List<int> { DateTime.Today.Year - member.DateOfBirth.Value.Year } : null,
                    RelationshipDurationDays = couple?.StartDate.HasValue == true
                        ? DateOnly.FromDateTime(DateTime.Today).DayNumber - couple.StartDate.Value.DayNumber
                        : 0,
                    Personality = couple.CouplePersonalityType != null
                        ? couple.CouplePersonalityType.Name
                        : (personalityMember != null ? personalityMember.ResultCode : null),
                    Mood = couple.CoupleMoodType != null ? couple.CoupleMoodType.Name : (member.MoodTypes != null ? member.MoodTypes.Name : null),
                    Interests = null, // TODO: Get interests of couple
                },
                VenueCandidates = aiCandidates
            };

            if (previewOnly == true)
            {
                return aiPromptRequest;
            }

            var promptJsonContext = JsonConverterUtil.Serialize(aiPromptRequest);

            var phase3SystemPrompt = BuildPhase3SystemPrompt(targetVenueCount);
            var p3Messages = new List<ChatMessage>()
            {
                new SystemChatMessage(phase3SystemPrompt),
                new UserChatMessage(promptJsonContext)
            };

            var p3Options = new ChatCompletionOptions 
            { 
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(), 
                Temperature = 0.5f 
            };

            try
            {
                var p3Result = await chatClient.CompleteChatAsync(p3Messages, p3Options);
                var responseText = p3Result.Value.Content[0].Text;

                var finalPlan = JsonConverterUtil.DeserializeOrDefault<AIDatePlanItemResponse>(responseText);

                finalPlan.Items = finalPlan.Items
                    .Where(i => i != null)
                    .GroupBy(i => i.VenueLocationId)
                    .Select(g => g.First())
                    .Take(targetVenueCount)
                    .ToList();

                if (!finalPlan.Items.Any())
                    throw new Exception("AI không thể tạo lịch trình từ dữ liệu này.");

                if (string.IsNullOrWhiteSpace(finalPlan.Reason))
                {
                    finalPlan.Reason = "Lịch trình được chọn theo khung giờ, ngân sách, sở thích và độ phù hợp trải nghiệm tổng thể.";
                }

                foreach (var item in finalPlan.Items)
                {
                    var rawVenue = rawVenues.FirstOrDefault(v => v.Id == item.VenueLocationId);
                    if (rawVenue != null)
                    {
                        item.VenueName = rawVenue.Name;
                        item.VenueDescription = rawVenue.Description;
                        item.VenueAddress = rawVenue.Address;
                        item.VenueAverageRating = rawVenue.AverageRating;
                        item.VenueCoverImage = DeserializeImages(rawVenue.CoverImage);
                    }
                        
                }

                return finalPlan;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi AI sinh lịch trình: {ex.Message}");
            }

            return aiCandidates;
        }

        private static int GetTargetVenueCount(DateTime plannedStartAtVn, DateTime plannedEndAtVn)
        {
            var totalMinutes = Math.Max(0, (plannedEndAtVn - plannedStartAtVn).TotalMinutes);

            // 1-2.5h => 1 điểm
            if (totalMinutes <= 150)
                return 1;

            // >2.5h đến 5h => 2 điểm
            if (totalMinutes <= 300)
                return 2;

            // >5h => 3 điểm
            return 3;
        }

        private static string BuildPhase1SystemPrompt(string validCategories)
        {
            return $@"Role: Dating Context Extractor.
Rules:
1. categories: Map input to closest semantic match in: [{validCategories}]. 
   Ex: 'uống bia'->'Bar/Pub', 'bánh ngọt'->'Cafe'. Unmappable -> [].
2. mood_tags: Extract vibes (chill, romantic...). Free text.
3. Output PURE JSON. No markdown, no yapping.

{{""categories"":[""MatchedCategory""],""mood_tags"":[""vibe""],""time_hint"":""Tối nay"",""special_note"":null}}";
        }

        private static string BuildPhase3SystemPrompt(int targetVenueCount)
        {
            targetVenueCount = Math.Clamp(targetVenueCount, 1, 3);

            return @$"Role: Date Planner AI.
Task: Map 'raw_query', 'user_intent', 'venue_candidates' -> JSON itinerary.

RULES (STRICT):
1. TIME CONSTRAINTS & TRAVEL:
   - Itinerary MUST fit strictly within [planned_start_at, planned_end_at].
   - Add 15-20 mins travel time between venues. Do NOT overlap times.
2. DYNAMIC SEQUENCING (COMMON SENSE):
   - IGNORE the text order in 'raw_query'. Sort venues based on human logic and time of day.
   - Core Heuristics: Morning (Breakfast/Cafe) -> Afternoon (Sightseeing/Outdoor/Sunset) -> Evening (Dinner/Main Meal) -> Late Night (Bar/Lounge/Live Music).
   - Match venue categories to their optimal operating hours within the user's timeframe.
3. ADAPTIVE SELECTION:
   - MUST output exactly {targetVenueCount} distinct venues.
   - If timeframe is short, prioritize fewer, better-matched stops.
4. FORMAT:
   - 'reason': exactly 1 sentence, Vietnamese.
        + MUST mention ALL selected venues (name if available, else venueLocationId).
        + MUST explain at least 2 concrete factors: budget fit, mood/intent, category, or rating.

        + DATA CONSTRAINT:
            - Only use information from the final selected 'items'.
            - Treat 'items' as the single source of truth.
            - DO NOT use or reference any venue from 'venue_candidates' that is not selected.

        + STRICT FORBIDDEN:
            - Any venue name or id not present in 'items'.
            - Any hallucinated or inferred venue.
   - 'note': < 15 words. Sharp and engaging. Vietnamese
   - OUTPUT PURE JSON. NO markdown. NO yapping.

{{""reason"":""string"",""items"":[{{""venueLocationId"":1,""startTime"":""HH:mm:ss"",""endTime"":""HH:mm:ss"",""note"":""string""}}]}}

";
        }
    }
}
