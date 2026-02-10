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

                var couple = await _unitOfWork.CoupleProfiles.GetByMemberIdAsync(member.Id);
                if (couple == null)
                    throw new Exception("Thành viên chưa thuộc cặp đôi nào");

                var datePlan = await _unitOfWork.DatePlans.GetByIdAndCoupleIdAsync(datePlanId, couple.id);
                if (datePlan == null)
                    throw new Exception("Không tìm thấy lịch trình buổi hẹn");

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

                var existedIds = requestVenueIds
                    .Where(id => existingVenueIds.Contains(id))
                    .Distinct()
                    .ToList();

                var planStartVn = TimezoneUtil.ToVietNamTime(datePlan.PlannedStartAt!.Value);
                var planEndVn = TimezoneUtil.ToVietNamTime(datePlan.PlannedEndAt!.Value);

                if (requestVenueIds.Count != requestVenueIds.Distinct().Count())
                    throw new Exception("Danh sách địa điểm bị trùng");

                if (requestVenueIds.Any(id => existingVenueIds.Contains(id)))
                    throw new Exception($"Một số địa điểm đã có trong lịch trình: {string.Join(", ", existedIds)}");

                var venuesWithTime = venues
                    .Where(v => v.StartTime.HasValue && v.EndTime.HasValue)
                    .ToList();

                var rangesNew = venuesWithTime.Select(v => new
                {
                    V = v,
                    Range = ResolveItemRangeWithinPlan(planStartVn, planEndVn, v.StartTime!.Value, v.EndTime!.Value)
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

                var maxOderIndex = datePlanItems
                    .Select(x => x.OrderIndex)
                    .Max() ?? 0;

                var items = new List<DatePlanItem>();             

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

                    datePlan.TotalCount += 1;
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

            throw new Exception("Khung giờ của mục này nằm ngoài thời gian của lịch trình buổi hẹn");
        }


        public async Task<int> DeleteDatePlanItemAsync(int value, int datePlanItemId, int datePlanId)
        {
            try
            {
                var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(value);
                if (member == null)
                    throw new Exception("Không tìm thấy hồ sơ thành viên");

                var couple = await _unitOfWork.CoupleProfiles.GetByMemberIdAsync(member.Id);
                if (couple == null)
                    throw new Exception("Thành viên chưa thuộc cặp đôi nào");

                var datePlan = await _unitOfWork.DatePlans.GetByIdAndCoupleIdAsync(datePlanId, couple.id);
                if (datePlan == null)
                    throw new Exception("Không tìm thấy lịch trình buổi hẹn");

                var datePlanItem = await _unitOfWork.DatePlanItems.GetByIdAndDatePlanIdAsync(datePlanItemId, datePlanId);
                if (datePlanItem == null)
                    throw new Exception("Không tìm thấy mục trong lịch trình");

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

                var couple = await _unitOfWork.CoupleProfiles.GetByMemberIdAsync(member.Id);
                if (couple == null)
                    throw new Exception("Thành viên chưa thuộc cặp đôi nào");

                var datePlan = await _unitOfWork.DatePlans.GetByIdAndCoupleIdAsync(datePlanId, couple.id);
                if (datePlan == null)
                    throw new Exception("Không tìm thấy lịch trình buổi hẹn");

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
                    throw new Exception("Không tìm thấy hồ sơ thành viên");

                var couple = await _unitOfWork.CoupleProfiles.GetByMemberIdAsync(member.Id);
                if (couple == null)
                    throw new Exception("Thành viên chưa thuộc cặp đôi nào");

                var datePlan = await _unitOfWork.DatePlans.GetByIdAndCoupleIdAsync(datePlanId, couple.id);
                if (datePlan == null)
                    throw new Exception("Không tìm thấy lịch trình buổi hẹn");

                var datePlanItem = await _unitOfWork.DatePlanItems.GetByIdAndDatePlanIdAsync(datePlanItemId, datePlanId, includeVenueLocation: true);
                if (datePlanItem == null)
                    throw new Exception("Không tìm thấy mục trong lịch trình");

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
                    throw new Exception("Không tìm thấy hồ sơ thành viên");

                var couple = await _unitOfWork.CoupleProfiles.GetByMemberIdAsync(member.Id);
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
                datePlan.Version += 1;

                _unitOfWork.DatePlanItems.Update(datePlanItem);
                _unitOfWork.DatePlans.Update(datePlan);
                var check = await _unitOfWork.SaveChangesAsync();
                if (check <= 0)
                    throw new Exception("Cập nhật mục trong lịch trình thất bại");

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

                var couple = await _unitOfWork.CoupleProfiles.GetByMemberIdAsync(member.Id);
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
    }
}
