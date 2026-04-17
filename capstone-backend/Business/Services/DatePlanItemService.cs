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
                    throw new Exception("Không tìm thấy hồ sơ thành viên");

                var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
                if (couple == null)
                    throw new Exception("Thành viên chưa thuộc cặp đôi nào");

                var datePlan = await _unitOfWork.DatePlans.GetByIdAndCoupleIdAsync(datePlanId, couple.id);
                if (datePlan == null)
                    throw new Exception("Không tìm thấy lịch trình buổi hẹn");

                if (datePlan.OrganizerMemberId != member.Id)
                    throw new Exception("Chỉ có người tổ chức buổi hẹn mới có thể chỉnh sửa mục lịch trình");

                if (datePlan.Status != DatePlanStatus.DRAFTED.ToString() &&
                    datePlan.Status != DatePlanStatus.PENDING.ToString())
                    throw new Exception("Chỉ có thể thêm địa điểm khi lịch trình ở trạng thái DRAFTED hoặc PENDING");

                // Check if date plan start and end time are valid
                var now = DateTime.UtcNow;
                if (!datePlan.PlannedStartAt.HasValue || !datePlan.PlannedEndAt.HasValue)
                    throw new Exception("Vui lòng thiết lập thời gian bắt đầu và kết thúc dự kiến trước khi thêm địa điểm");

                // Snapshot
                var venues = request.Venues ?? new List<DatePlanItemRequest>();
                if (venues == null || !venues.Any())
                    throw new Exception("Danh sách địa điểm không được để trống");

                // Check if only distinct venue locations are added
                var datePlanItems = await _unitOfWork.DatePlanItems.GetByDatePlanIdAsync(datePlanId);
                var existingVenueIds = datePlanItems.Select(dpi => dpi.VenueLocationId).ToHashSet();

                var requestVenueIds = request.Venues
                    .Select(x => x.VenueLocationId)
                    .ToList();

                if (requestVenueIds.Count != requestVenueIds.Distinct().Count())
                    throw new Exception("Danh sách địa điểm bị trùng");

                var existedIds = requestVenueIds
                    .Where(id => existingVenueIds.Contains(id))
                    .Distinct()
                    .ToList();

                if (existedIds.Any())
                {
                    var existedVenues = await _unitOfWork.VenueLocations.GetAsync(v => existedIds.Contains(v.Id));

                    var existedVenueMap = existedVenues.ToDictionary(v => v.Id, v => v.Name);

                    var existedVenueDisplay = existedIds
                        .Select(id =>
                            existedVenueMap.TryGetValue(id, out var venueName) && !string.IsNullOrWhiteSpace(venueName)
                                ? $"{venueName} ({id})"
                                : $"ID: {id}")
                        .ToList();

                    throw new Exception($"Một số địa điểm đã có trong lịch trình: {string.Join(", ", existedVenueDisplay)}");
                }

                var venueLocations = await _unitOfWork.VenueLocations.GetAsync(
                    v => requestVenueIds.Contains(v.Id) &&
                         v.IsDeleted == false &&
                         v.Status == VenueLocationStatus.ACTIVE.ToString()
                );

                var activeVenueIds = venueLocations
                    .Select(v => v.Id)
                    .ToHashSet();

                var invalidVenueIds = requestVenueIds
                    .Where(id => !activeVenueIds.Contains(id))
                    .Distinct()
                    .ToList();

                if (invalidVenueIds.Any())
                {
                    var invalidVenues = await _unitOfWork.VenueLocations.GetAsync(v => invalidVenueIds.Contains(v.Id));

                    var invalidVenueMap = invalidVenues.ToDictionary(v => v.Id, v => v.Name);

                    var invalidVenueDisplay = invalidVenueIds
                        .Select(id =>
                            invalidVenueMap.TryGetValue(id, out var venueName) && !string.IsNullOrWhiteSpace(venueName)
                                ? $"{venueName} ({id})"
                                : $"ID: {id}")
                        .ToList();

                    throw new Exception($"Một số địa điểm không tồn tại hoặc không hoạt động: {string.Join(", ", invalidVenueDisplay)}");
                }

                var planStartVn = TimezoneUtil.ToVietNamTime(datePlan.PlannedStartAt!.Value);
                var planEndVn = TimezoneUtil.ToVietNamTime(datePlan.PlannedEndAt!.Value);

                foreach (var v in venues)
                {
                    if (!v.StartTime.HasValue || !v.EndTime.HasValue)
                        throw new Exception("Mỗi mục trong lịch trình phải có đầy đủ thời gian bắt đầu và kết thúc");
                }

                var rangesNew = venues.Select(v => new
                {
                    V = v,
                    Range = ResolveItemRangeWithinPlan(
                        planStartVn,
                        planEndVn,
                        v.StartTime!.Value,
                        v.EndTime!.Value
                    )
                }).ToList();

                // new - new
                for (int i = 0; i < rangesNew.Count; i++)
                    for (int j = i + 1; j < rangesNew.Count; j++)
                    {
                        if (Overlap(rangesNew[i].Range, rangesNew[j].Range))
                            throw new Exception($"Khung giờ trong danh sách bạn chọn đang bị trùng nhau");
                    }

                var existingWithTime = datePlanItems
                    .Where(x => x.StartTime.HasValue && x.EndTime.HasValue)
                    .Select(x => new
                    {
                        Item = x,
                        Range = ResolveItemRangeWithinPlan(planStartVn, planEndVn, x.StartTime!.Value, x.EndTime!.Value)
                    })
                    .ToList();

                foreach (var n in rangesNew)
                    foreach (var ex in existingWithTime)
                    {
                        if (Overlap(n.Range, ex.Range))
                            throw new Exception($"Khung giờ bạn chọn bị trùng với một mục đã có trong lịch trình");
                    }

                var items = new List<DatePlanItem>();

                foreach (var v in venues)
                {
                    var item = _mapper.Map<DatePlanItem>(v);
                    item.DatePlanId = datePlanId;
                    items.Add(item);
                }

                await _unitOfWork.BeginTransactionAsync();

                await _unitOfWork.DatePlanItems.AddRangeAsync(items);

                datePlan.TotalCount += items.Count;
                datePlan.UpdatedAt = DateTime.UtcNow;
                datePlan.Version += 1;

                _unitOfWork.DatePlans.Update(datePlan);

                await _unitOfWork.SaveChangesAsync();

                await RebuildOrderIndexByTimeAsync(datePlanId, planStartVn, planEndVn);

                var result = await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return result;
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
            var safePlanStart = new DateTime(planStartVn.Year, planStartVn.Month, planStartVn.Day, planStartVn.Hour, planStartVn.Minute, 0);
            var safePlanEnd = new DateTime(planEndVn.Year, planEndVn.Month, planEndVn.Day, planEndVn.Hour, planEndVn.Minute, 0);

            var day0 = DateOnly.FromDateTime(safePlanStart);
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

                if (start >= safePlanStart && end <= safePlanEnd && end > start)
                    return (start, end);
            }

            throw new Exception("Khung giờ của mục này nằm ngoài thời gian của lịch trình buổi hẹn");
        }


        public async Task<int> DeleteDatePlanItemAsync(int value, int datePlanItemId, int datePlanId)
        {
            try
            {
                var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(value);
                if (member == null)
                    throw new Exception("Không tìm thấy hồ sơ thành viên");

                var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
                if (couple == null)
                    throw new Exception("Thành viên chưa thuộc cặp đôi nào");

                var datePlan = await _unitOfWork.DatePlans.GetByIdAndCoupleIdAsync(datePlanId, couple.id);
                if (datePlan == null)
                    throw new Exception("Không tìm thấy lịch trình buổi hẹn");

                var datePlanItem = await _unitOfWork.DatePlanItems.GetByIdAndDatePlanIdAsync(datePlanItemId, datePlanId);
                if (datePlanItem == null)
                    throw new Exception("Không tìm thấy mục trong lịch trình");

                if (datePlan.OrganizerMemberId != member.Id)
                    throw new Exception("Chỉ có người tổ chức buổi hẹn mới có thể chỉnh sửa mục lịch trình");

                datePlan.TotalCount = Math.Max(0, datePlan.TotalCount - 1);
                datePlan.UpdatedAt = DateTime.UtcNow;
                datePlan.Version += 1;

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
                    throw new Exception("Không tìm thấy hồ sơ thành viên");

                var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
                if (couple == null)
                    throw new Exception("Thành viên chưa thuộc cặp đôi nào");

                var datePlan = await _unitOfWork.DatePlans.GetByIdAndCoupleIdAsync(datePlanId, couple.id);
                if (datePlan == null)
                    throw new Exception("Không tìm thấy lịch trình buổi hẹn");

                if (datePlan.Status == DatePlanStatus.DRAFTED.ToString() && datePlan.OrganizerMemberId != member.Id)
                    throw new Exception("Chỉ có người tổ chức buổi hẹn mới có thể xem lịch trình ở trạng thái DRAFTED");

                var items = await _unitOfWork.DatePlanItems.GetByDatePlanIdAsync(datePlanId, true);

                var activeItems = items
                    .Where(x => x.IsDeleted == false)
                    .ToList();

                if (datePlan.PlannedStartAt.HasValue && datePlan.PlannedEndAt.HasValue)
                {
                    var planStartVn = TimezoneUtil.ToVietNamTime(datePlan.PlannedStartAt.Value);
                    var planEndVn = TimezoneUtil.ToVietNamTime(datePlan.PlannedEndAt.Value);

                    activeItems = activeItems
                        .OrderBy(x =>
                        {
                            if (!x.StartTime.HasValue || !x.EndTime.HasValue)
                                return DateTime.MaxValue;

                            return ResolveItemRangeWithinPlan(
                                planStartVn,
                                planEndVn,
                                x.StartTime.Value,
                                x.EndTime.Value
                            ).Start;
                        })
                        .ThenBy(x =>
                        {
                            if (!x.StartTime.HasValue || !x.EndTime.HasValue)
                                return DateTime.MaxValue;

                            return ResolveItemRangeWithinPlan(
                                planStartVn,
                                planEndVn,
                                x.StartTime.Value,
                                x.EndTime.Value
                            ).End;
                        })
                        .ThenBy(x => x.Id)
                        .ToList();
                }
                else
                {
                    activeItems = activeItems
                        .OrderBy(x => x.OrderIndex ?? int.MaxValue)
                        .ThenBy(x => x.Id)
                        .ToList();
                }

                var totalCount = activeItems.Count;

                var pagedItems = activeItems
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return new PagedResult<DatePlanItemResponse>
                {
                    Items = _mapper.Map<List<DatePlanItemResponse>>(pagedItems),
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
                    throw new Exception("Không tìm thấy hồ sơ thành viên");

                var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
                if (couple == null)
                    throw new Exception("Thành viên chưa thuộc cặp đôi nào");

                var datePlan = await _unitOfWork.DatePlans.GetByIdAndCoupleIdAsync(datePlanId, couple.id);
                if (datePlan == null)
                    throw new Exception("Không tìm thấy lịch trình buổi hẹn");

                var datePlanItem = await _unitOfWork.DatePlanItems.GetByIdAndDatePlanIdAsync(datePlanItemId, datePlanId, includeVenueLocation: true);
                if (datePlanItem == null)
                    throw new Exception("Không tìm thấy mục trong lịch trình");

                if (datePlan.Status == DatePlanStatus.DRAFTED.ToString() && datePlan.OrganizerMemberId != member.Id)
                    throw new Exception("Chỉ có người tổ chức buổi hẹn mới có thể xem lịch trình ở trạng thái DRAFTED");

                var response = _mapper.Map<DatePlanItemResponse>(datePlanItem);
                response.Version = datePlan.Version;
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
                    throw new Exception("Không tìm thấy hồ sơ thành viên");

                var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
                if (couple == null)
                    throw new Exception("Thành viên chưa thuộc cặp đôi nào");

                var datePlan = await _unitOfWork.DatePlans.GetByIdAndCoupleIdAsync(datePlanId, couple.id);
                if (datePlan == null)
                    throw new Exception("Không tìm thấy lịch trình buổi hẹn");

                var datePlanItem = await _unitOfWork.DatePlanItems.GetByIdAndDatePlanIdAsync(datePlanItemId, datePlanId);
                if (datePlanItem == null)
                    throw new Exception("Không tìm thấy mục trong lịch trình");

                if (datePlan.Status != DatePlanStatus.DRAFTED.ToString() &&
                    datePlan.Status != DatePlanStatus.PENDING.ToString())
                    throw new Exception("Chỉ có thể cập nhật lịch trình ở trạng thái DRAFTED hoặc PENDING");

                // Concurrency check
                if (datePlan.Version != version)
                    throw new Exception("Lịch trình đã được chỉnh sửa bởi người khác. Vui lòng tải lại và thử lại");

                if (!datePlan.PlannedStartAt.HasValue || !datePlan.PlannedEndAt.HasValue)
                    throw new Exception("Vui lòng thiết lập thời gian bắt đầu và kết thúc dự kiến trước khi cập nhật mục lịch trình");

                var finalStartTime = request.StartTime ?? datePlanItem.StartTime;
                var finalEndTime = request.EndTime ?? datePlanItem.EndTime;

                if (!finalStartTime.HasValue || !finalEndTime.HasValue)
                    throw new Exception("Mục lịch trình phải có đầy đủ thời gian bắt đầu và kết thúc");

                var planStartVn = TimezoneUtil.ToVietNamTime(datePlan.PlannedStartAt.Value);
                var planEndVn = TimezoneUtil.ToVietNamTime(datePlan.PlannedEndAt.Value);

                await ValidateDatePlanItemTimeAsync(
                    datePlanId,
                    datePlanItemId,
                    planStartVn,
                    planEndVn,
                    finalStartTime.Value,
                    finalEndTime.Value
                );

                datePlanItem.StartTime = finalStartTime.Value;
                datePlanItem.EndTime = finalEndTime.Value;

                if (request.Note != null)
                    datePlanItem.Note = request.Note;

                datePlan.UpdatedAt = DateTime.UtcNow;
                datePlan.Version += 1;

                await _unitOfWork.BeginTransactionAsync();

                _unitOfWork.DatePlanItems.Update(datePlanItem);
                _unitOfWork.DatePlans.Update(datePlan);

                await _unitOfWork.SaveChangesAsync();

                await RebuildOrderIndexByTimeAsync(datePlanId, planStartVn, planEndVn);

                var check = await _unitOfWork.SaveChangesAsync();
                if (check <= 0)
                    throw new Exception("Cập nhật mục trong lịch trình thất bại");

                await _unitOfWork.CommitTransactionAsync();

                datePlanItem = await _unitOfWork.DatePlanItems.GetByIdAndDatePlanIdAsync(datePlanItemId, datePlanId, includeVenueLocation: true);

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
                    throw new Exception("Danh sách thứ tự không được để trống");

                if (orderedIds.Count != orderedIds.Distinct().Count())
                    throw new Exception("Danh sách thứ tự bị trùng lặp");

                // Auth check
                var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
                if (member == null)
                    throw new Exception("Không tìm thấy hồ sơ thành viên");

                var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
                if (couple == null)
                    throw new Exception("Thành viên chưa thuộc cặp đôi nào");

                var datePlan = await _unitOfWork.DatePlans.GetByIdAndCoupleIdAsync(datePlanId, couple.id);
                if (datePlan == null)
                    throw new Exception("Không tìm thấy lịch trình buổi hẹn");

                var datePlanItems = await _unitOfWork.DatePlanItems.GetByDatePlanIdAsync(datePlanId);

                if (orderedIds.Count != datePlanItems.Count())
                    throw new Exception("Danh sách thứ tự phải chứa tất cả các mục trong lịch trình");

                var activeIdSet = datePlanItems
                    .Select(x => x.Id)
                    .ToHashSet();
                var invalidIds = orderedIds
                    .Where(id => !activeIdSet.Contains(id))
                    .ToList();
                if (invalidIds.Any())
                    throw new Exception($"Một số mục không thuộc lịch trình này: {string.Join(", ", invalidIds)}");

                await _unitOfWork.BeginTransactionAsync();

                // Sort items to take old time
                var sortedCurrentItems = datePlanItems
                    .OrderBy(x => x.OrderIndex)
                    .ToList();

                // Save old time
                var oldTimes = sortedCurrentItems
                    .Select(x => new { x.StartTime, x.EndTime })
                    .ToList();

                var newIndexMap = orderedIds
                    .Select((id, idx) => new { id, idx })
                    .ToDictionary(x => x.id, x => x.idx);

                foreach (var item in datePlanItems)
                {
                    if (newIndexMap.TryGetValue(item.Id, out int newIndex))
                    {
                        item.OrderIndex = newIndex + 1;

                        item.StartTime = oldTimes[newIndex].StartTime;
                        item.EndTime = oldTimes[newIndex].EndTime;
                    }
                }

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

        private static bool Overlap((DateTime s, DateTime e) a, (DateTime s, DateTime e) b)
            => a.s < b.e && b.s < a.e;

        private async Task ValidateDatePlanItemTimeAsync(
            int datePlanId,
            int? excludeDatePlanItemId,
            DateTime planStartVn,
            DateTime planEndVn,
            TimeOnly startTime,
            TimeOnly endTime)
        {
            var newRange = ResolveItemRangeWithinPlan(
                planStartVn,
                planEndVn,
                startTime,
                endTime
            );

            var existingItems = await _unitOfWork.DatePlanItems.GetByDatePlanIdAsync(datePlanId);

            var existingWithTime = existingItems
                .Where(x => x.Id != excludeDatePlanItemId &&
                            x.IsDeleted == false &&
                            x.StartTime.HasValue &&
                            x.EndTime.HasValue)
                .Select(x => new
                {
                    Item = x,
                    Range = ResolveItemRangeWithinPlan(
                        planStartVn,
                        planEndVn,
                        x.StartTime!.Value,
                        x.EndTime!.Value
                    )
                })
                .ToList();

            foreach (var ex in existingWithTime)
            {
                if (Overlap(newRange, ex.Range))
                    throw new Exception("Khung giờ bạn chọn bị trùng với một mục đã có trong lịch trình");
            }
        }

        private async Task RebuildOrderIndexByTimeAsync(int datePlanId, DateTime planStartVn, DateTime planEndVn)
        {
            var items = await _unitOfWork.DatePlanItems.GetByDatePlanIdAsync(datePlanId);

            var sortableItems = items
                .Where(x => x.IsDeleted == false)
                .OrderBy(x =>
                {
                    if (!x.StartTime.HasValue || !x.EndTime.HasValue)
                        return DateTime.MaxValue;

                    return ResolveItemRangeWithinPlan(
                        planStartVn,
                        planEndVn,
                        x.StartTime.Value,
                        x.EndTime.Value
                    ).Start;
                })
                .ThenBy(x =>
                {
                    if (!x.StartTime.HasValue || !x.EndTime.HasValue)
                        return DateTime.MaxValue;

                    return ResolveItemRangeWithinPlan(
                        planStartVn,
                        planEndVn,
                        x.StartTime.Value,
                        x.EndTime.Value
                    ).End;
                })
                .ThenBy(x => x.Id)
                .ToList();

            for (int i = 0; i < sortableItems.Count; i++)
            {
                sortableItems[i].OrderIndex = i + 1;
            }

            _unitOfWork.DatePlanItems.UpdateRange(sortableItems);
        }
    }
}
