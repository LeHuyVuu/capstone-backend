using AutoMapper;
using capstone_backend.Business.Common.Constants;
using capstone_backend.Business.Common.Helpers;
using capstone_backend.Business.DTOs.Challenge;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Extensions.Common;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace capstone_backend.Business.Services
{
    public class ChallengeService : IChallengeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ChallengeService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ChallengeResponse> CreateChallengeAsyncV2(int userId, CreateChallengeRequest request)
        {
            // 1. Validate Input
            Validate(request);

            // 2. Normalize DateTime to UTC
            if (request.StartDate.HasValue)
                request.StartDate = DateTimeNormalizeUtil.NormalizeToUtc(request.StartDate.Value);
            if (request.EndDate.HasValue)
                request.EndDate = DateTimeNormalizeUtil.NormalizeToUtc(request.EndDate.Value);

            // 3. Map to Entity
            var challenge = _mapper.Map<Challenge>(request);

            // 4. Process Dynamic Rules and get ConditionRules JSON + possibly updated TargetGoal
            var (conditionRules, updatedTargetGoal) = await ProcessDynamicRulesAsync(
                request.TriggerEvent,
                request.GoalMetric,
                request.RuleData
            );

            // Assign Json after processing rules
            challenge.ConditionRules = conditionRules;

            // Update TargetGoal if UNIQUE_LIST
            if (request.GoalMetric == ChallengeConstants.GoalMetrics.UNIQUE_LIST && updatedTargetGoal > 0)
            {
                challenge.TargetGoal = updatedTargetGoal;
            }

            // 5. Set Default Status
            challenge.Status = ChallengeStatus.INACTIVE.ToString();

            // 6. Save to DB
            await _unitOfWork.Challenges.AddAsync(challenge);
            await _unitOfWork.SaveChangesAsync();

            // 7. Enrich Response
            var response = await EnrichChallengeResponseAsync(new List<Challenge> { challenge });
            return response.First();
        }

        public async Task<int> DeleteChallengeAsync(int challengeId)
        {
            var challenge = await _unitOfWork.Challenges.GetByIdAsync(challengeId);
            if (challenge == null || (challenge.IsDeleted.HasValue && challenge.IsDeleted != false))
                throw new Exception("Thử thách không tồn tại");

            if (challenge.Status == ChallengeStatus.ACTIVE.ToString())
                throw new Exception("Không thể xóa thử thách đang diễn ra (ACTIVE). Vui lòng hủy kích hoạt thử thách trước khi xóa.");

            if (challengeId == 14)
                throw new Exception("Thử thách đặc biệt này không thể bị xóa");

            challenge.IsDeleted = true;
            _unitOfWork.Challenges.Update(challenge);
            return await _unitOfWork.SaveChangesAsync();
        }

        public async Task<PagedResult<ChallengeResponse>> GetAllChallengesAsync(int pageNumber, int pageSize)
        {
            var (challenges, totalCount) = await _unitOfWork.Challenges.GetPagedAsync(
                    pageNumber,
                    pageSize,
                    c => c.IsDeleted == false,
                    c => c.OrderByDescending(ch => ch.CreatedAt)
                );

            var enrichedChallenges = await EnrichChallengeResponseAsync(challenges);

            return new PagedResult<ChallengeResponse>
            {
                Items = enrichedChallenges,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        private void Validate(CreateChallengeRequest request)
        {
            if (request.RewardPoints <= 0)
                throw new Exception("Điểm thưởng phải là số dương lớn hơn 0");

            if (request.GoalMetric == ChallengeConstants.GoalMetrics.UNIQUE_LIST && request.TargetGoal <= 0)
                throw new Exception("Mục tiêu số lượng phải lớn hơn 0");

            if (!Enum.TryParse<ChallengeTriggerEvent>(request.TriggerEvent, out _))
                throw new Exception($"TriggerEvent '{request.TriggerEvent}' không hợp lệ");

            var validMetrics = ChallengeConstants.AllowedGoalMetrics;
            if (!validMetrics.Contains(request.GoalMetric))
                throw new Exception($"GoalMetric '{request.GoalMetric}' không hợp lệ");

            if (request.StartDate.HasValue)
            {
                if (request.StartDate.Value < DateTime.UtcNow.AddMinutes(-5))
                    throw new Exception("Ngày bắt đầu không được nằm trong quá khứ");

                if (request.EndDate.HasValue && request.StartDate.Value >= request.EndDate.Value)
                    throw new Exception("Ngày bắt đầu phải trước ngày kết thúc");
            }
        }

        private void Validate(UpdateChallengeRequest request, Challenge existingChallenge)
        {
            if (request.RewardPoints <= 0)
                throw new Exception("Điểm thưởng phải là số dương lớn hơn 0");

            if (request.GoalMetric != ChallengeConstants.GoalMetrics.UNIQUE_LIST && request.TargetGoal <= 0)
                throw new Exception("Mục tiêu số lượng phải lớn hơn 0");

            if (!Enum.TryParse<ChallengeTriggerEvent>(request.TriggerEvent, out _))
                throw new Exception($"TriggerEvent '{request.TriggerEvent}' không hợp lệ");

            var validMetrics = ChallengeConstants.AllowedGoalMetrics;
            if (!validMetrics.Contains(request.GoalMetric))
                throw new Exception($"GoalMetric '{request.GoalMetric}' không hợp lệ");

            if (!Enum.TryParse<ChallengeStatus>(request.Status, out _))
                throw new Exception($"Trạng thái (Status) '{request.Status}' không hợp lệ");

            if (request.StartDate.HasValue)
            {
                if (existingChallenge.StartDate > DateTime.UtcNow &&
                    request.StartDate.Value < DateTime.UtcNow.AddMinutes(-5))
                {
                    throw new Exception("Không thể lùi ngày bắt đầu về quá khứ");
                }

                if (request.EndDate.HasValue && request.StartDate.Value >= request.EndDate.Value)
                    throw new Exception("Ngày bắt đầu phải trước ngày kết thúc");
            }
        }

        private async Task<(string ConditionRules, int UpdatedTargetGoal)> ProcessDynamicRulesAsync(
            string triggerEvent,
            string goalMetric,
            Dictionary<string, object> ruleData)
        {
            int updatedTargetGoal = 0;
            if (ruleData == null || !ruleData.Any())
            {
                if (goalMetric == ChallengeConstants.GoalMetrics.UNIQUE_LIST)
                {
                    throw new Exception($"Mục tiêu '{ChallengeConstants.GoalMetrics.UNIQUE_LIST}' bắt buộc phải đi kèm điều kiện chọn quán (venue_id). Loại sự kiện này không hỗ trợ hoặc bạn chưa chọn quán nào!");
                }

                var emptyJson = JsonSerializer.Serialize(new
                {
                    logic = "AND",
                    rules = new List<object>()
                });
                return (emptyJson, 0);
            }

            var allowedKeys = new Dictionary<string, List<string>>
            {
                { ChallengeTriggerEvent.REVIEW.ToString(), new List<string> { ChallengeConstants.RuleKeys.VENUE_ID, ChallengeConstants.RuleKeys.HAS_IMAGE } },
                { ChallengeTriggerEvent.POST.ToString(), new List<string> { ChallengeConstants.RuleKeys.HASH_TAG, ChallengeConstants.RuleKeys.HAS_IMAGE } },
                { ChallengeTriggerEvent.CHECKIN.ToString(), new List<string> { ChallengeConstants.RuleKeys.VENUE_ID } }
            };

            var whiteList = allowedKeys.GetValueOrDefault(triggerEvent) ?? new List<string>();
            var ruleList = new List<object>();

            foreach (var item in ruleData)
            {
                if (item.Value == null)
                    continue;
                if (!whiteList.Contains(item.Key))
                    throw new Exception($"Key '{item.Key}' không được cho phép trong event '{triggerEvent}'.");

                string key = item.Key;
                object rawValue = item.Value;
                string op = ChallengeConstants.RuleOps.Eq;

                if (rawValue is JsonElement element)
                {
                    switch (element.ValueKind)
                    {
                        case JsonValueKind.Array:
                            op = ChallengeConstants.RuleOps.In;
                            if (key == ChallengeConstants.RuleKeys.VENUE_ID)
                            {
                                var intList = JsonSerializer.Deserialize<List<string>>(element.GetRawText());
                                if (intList != null && intList.Any())
                                {
                                    var uniqueIds = intList.Distinct().ToList();
                                    var invalidIds = await _unitOfWork.VenueLocations.GetInvalidVenueIdsAsync(uniqueIds);
                                    if (invalidIds.Any())
                                    {
                                        throw new Exception($"Các địa điểm sau không tồn tại hoặc chưa kích hoạt: {string.Join(", ", invalidIds)}");
                                    }

                                    rawValue = uniqueIds;
                                    if (goalMetric == ChallengeConstants.GoalMetrics.UNIQUE_LIST)
                                        updatedTargetGoal = uniqueIds.Count;
                                }
                                else
                                {
                                    rawValue = new List<string>();
                                }
                            }
                            else
                            {
                                rawValue = JsonSerializer.Deserialize<List<object>>(element.GetRawText());
                            }
                            break;
                        case JsonValueKind.Number:
                            rawValue = element.GetDecimal();
                            break;
                        case JsonValueKind.True:
                        case JsonValueKind.False:
                            rawValue = element.GetBoolean();
                            break;
                        default:
                            rawValue = element.ToString();
                            break;
                    }
                }
                ruleList.Add(new
                {
                    key = key,
                    op = op,
                    value = rawValue
                });
            }

            var finalJson = JsonSerializer.Serialize(new
            {
                logic = "AND",
                rules = ruleList
            });

            if (goalMetric == ChallengeConstants.GoalMetrics.UNIQUE_LIST && updatedTargetGoal <= 0)
            {
                throw new Exception($"Mục tiêu '{ChallengeConstants.GoalMetrics.UNIQUE_LIST}' (Số địa điểm khác nhau) bắt buộc phải đi kèm điều kiện chọn quán (venue_id). Loại sự kiện '{triggerEvent}' không hỗ trợ hoặc bạn chưa chọn quán nào!");
            }

            return (finalJson, updatedTargetGoal);
        }

        private async Task<List<ChallengeResponse>> EnrichChallengeResponseAsync(IEnumerable<Challenge> challenges)
        {
            var challengeResponses = _mapper.Map<List<ChallengeResponse>>(challenges);
            var allVenueIds = new List<string>();

            // Extract all venue IDs from rules
            foreach (var challenge in challenges)
            {
                if (string.IsNullOrEmpty(challenge.ConditionRules))
                    continue;

                try
                {
                    var ruleWrapper = JsonSerializer.Deserialize<ChallengeRuleWrapper>(challenge.ConditionRules);
                    if (ruleWrapper?.Rules != null)
                    {
                        foreach (var rule in ruleWrapper.Rules)
                        {
                            if (rule.Key == ChallengeConstants.RuleKeys.VENUE_ID && rule.Value is JsonElement valElement)
                            {
                                var ids = JsonSerializer.Deserialize<List<string>>(valElement);
                                if (ids != null)
                                    allVenueIds.AddRange(ids);
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    // Log error and skip malformed rules
                    continue;
                }
            }

            // Get venue names
            var venueLookup = await _unitOfWork.VenueLocations.GetNamesByIdsAsync(allVenueIds);
            var venueIdToName = venueLookup.ToDictionary(v => v.Id.ToString(), v => v.Name);

            var items = challenges.ToList();

            // Turn rules to dto foreach challenge
            for (int i = 0; i < items.Count; i++)
            {
                var rawRules = items[i].ConditionRules;
                var targetDto = challengeResponses[i];

                targetDto.RuleData = new Dictionary<string, object>();
                targetDto.Instructions = new List<string>();

                if (string.IsNullOrEmpty(rawRules))
                    continue;

                if (targetDto.TriggerEvent == ChallengeTriggerEvent.CHECKIN.ToString())
                {
                    targetDto.Instructions.Add("📍 Thử thách yêu cầu điểm danh hằng ngày");
                }

                try
                {
                    var ruleWrapper = JsonSerializer.Deserialize<ChallengeRuleWrapper>(rawRules);
                    if (ruleWrapper?.Rules != null)
                    {
                        foreach (var r in ruleWrapper.Rules)
                        {
                            targetDto.RuleData[r.Key] = r.Value;

                            if (r.Key == ChallengeConstants.RuleKeys.VENUE_ID)
                            {
                                if (r.Value is JsonElement valElement && valElement.ValueKind == JsonValueKind.Array)
                                {
                                    var ids = JsonSerializer.Deserialize<List<string>>(valElement);
                                    var names = ids?.Select(id => venueIdToName.ContainsKey(id) ? venueIdToName[id] : id.ToString());
                                    var venueNameStr = names != null ? string.Join(", ", names) : "N/A";

                                    targetDto.Instructions.Add($"📍 Thử thách yêu cầu check-in tại địa điểm: {venueNameStr}");
                                }
                            }
                            else if (r.Key == ChallengeConstants.RuleKeys.HAS_IMAGE && r.Value is JsonElement val && val.ValueKind == JsonValueKind.True)
                            {
                                targetDto.Instructions.Add("📸 Bắt buộc đính kèm hình ảnh.");
                            }
                            else if (r.Key == ChallengeConstants.RuleKeys.HASH_TAG)
                            {
                                targetDto.Instructions.Add($"🏷️ Phải có hashtag: {r.Value}");
                            }
                        }
                    }
                }
                catch (JsonException)
                {

                    continue;
                }
            }

            return challengeResponses;
        }

        public async Task<ChallengeResponse> UpdateChallengeAsync(int challengeId, UpdateChallengeRequest request)
        {
            var challenge = await _unitOfWork.Challenges.GetByIdAsync(challengeId);
            if (challenge == null || (challenge.IsDeleted.HasValue && challenge.IsDeleted != false))
                throw new Exception("Thử thách không tồn tại");

            Validate(request, challenge);

            bool isModifyingRules = request.RuleData != null || request.TargetGoal != challenge.TargetGoal || request.TriggerEvent != challenge.TriggerEvent;
            if (challenge.Status == ChallengeStatus.ACTIVE.ToString() && isModifyingRules)
                throw new Exception("Không thể thay đổi luật, mục tiêu hoặc loại sự kiện khi Thử thách đang diễn ra (ACTIVE). Chỉ có thể sửa tên và mô tả");

            _mapper.Map(request, challenge);

            if (request.StartDate.HasValue)
                challenge.StartDate = DateTimeNormalizeUtil.NormalizeToUtc(request.StartDate.Value);
            if (request.EndDate.HasValue)
                challenge.EndDate = DateTimeNormalizeUtil.NormalizeToUtc(request.EndDate.Value);

            if (request.RuleData != null)
            {
                var (conditionRules, updatedTargetGoal) = await ProcessDynamicRulesAsync(
                    request.TriggerEvent ?? challenge.TriggerEvent,
                    request.GoalMetric ?? challenge.GoalMetric,
                    request.RuleData
                );

                challenge.ConditionRules = conditionRules;

                if ((request.GoalMetric ?? challenge.GoalMetric) == ChallengeConstants.GoalMetrics.UNIQUE_LIST && updatedTargetGoal > 0)
                {
                    challenge.TargetGoal = updatedTargetGoal;
                }
            }

            challenge.Status = request.Status ?? challenge.Status;
            challenge.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Challenges.Update(challenge);
            await _unitOfWork.SaveChangesAsync();

            var response = await EnrichChallengeResponseAsync(new List<Challenge> { challenge });
            return response.First();
        }

        public async Task<ChallengeResponse> GetChallengeByIdAsync(int challengeId)
        {
            var challenge = await _unitOfWork.Challenges.GetByIdAsync(challengeId);
            if (challenge == null || (challenge.IsDeleted.HasValue && challenge.IsDeleted != false))
                throw new Exception("Thử thách không tồn tại");

            var enriched = await EnrichChallengeResponseAsync(new List<Challenge> { challenge });
            return enriched.First();
        }

        public async Task<int> ChangeChallengeStatusAsync(int challengeId, string newStatus)
        {
            var challenge = await _unitOfWork.Challenges.GetByIdAsync(challengeId);
            if (challenge == null || (challenge.IsDeleted.HasValue && challenge.IsDeleted != false))
                throw new Exception("Thử thách không tồn tại");

            if (!Enum.TryParse<ChallengeStatus>(newStatus, out _))
                throw new Exception($"Trạng thái (Status) '{newStatus}' không hợp lệ");

            // Check same status
            if (challenge.Status == newStatus)
                throw new Exception($"Thử thách đã ở trạng thái '{newStatus}'");

            challenge.Status = newStatus;
            challenge.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            return challenge.Id;
        }

        private class ChallengeRuleWrapper
        {
            [JsonPropertyName("logic")]
            public string Logic { get; set; }

            [JsonPropertyName("rules")]
            public List<ChallengeRuleItem> Rules { get; set; }
        }

        private class ChallengeRuleItem
        {
            [JsonPropertyName("key")]
            public string Key { get; set; }

            [JsonPropertyName("op")]
            public string Op { get; set; }

            [JsonPropertyName("value")]
            public object Value { get; set; }
        }

        public async Task<PagedResult<MemberChallengeResponse>> GetMemberChallengesAsync(int userId, int pageNumber, int pageSize)
        {
            // Find member profile
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            // Find couple profile
            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
            if (couple == null)
                throw new Exception("Thành viên chưa thuộc cặp đôi nào");

            var now = DateTime.UtcNow;

            // Get active challenges
            var (challenges, totalCount) = await _unitOfWork.Challenges.GetPagedAsync(
                pageNumber,
                pageSize,
                c => c.IsDeleted == false &&
                     c.Status == ChallengeStatus.ACTIVE.ToString() &&
                     (c.StartDate == null || c.StartDate <= now) &&
                     (c.EndDate == null || c.EndDate >= now),
                c => c.OrderByDescending(c => c.CreatedAt)
            );

            // Enrich challenge response
            var enriched = await EnrichChallengeResponseAsync(challenges);
            var result = enriched.Select(x => new MemberChallengeResponse
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                TriggerEvent = x.TriggerEvent,
                RewardPoints = x.RewardPoints,
                GoalMetric = x.GoalMetric,
                TargetGoal = x.TargetGoal,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Status = x.Status,
                RuleData = x.RuleData,
                Instructions = x.Instructions,
                CreatedAt = x.CreatedAt,

                IsJoined = false
            }).ToList();

            result = result
                .OrderByDescending(x => x.Id == 14)
                .ThenByDescending(x => x.CreatedAt)
                .ToList();

            // Get joined challenges for this couple if exists
            if (couple != null && result.Any())
            {
                var challengeIds = result.Select(r => r.Id).ToList();

                var joinedRows = await _unitOfWork.CoupleProfileChallenges.GetByCoupleIdAndChallengeIdsAsync(couple.id, challengeIds);
                var map = joinedRows.ToDictionary(x => x.ChallengeId, x => x);

                foreach (var item in result)
                {
                    if (map.TryGetValue(item.Id, out var cc))
                    {
                        item.IsJoined = true;
                        item.CoupleChallengeId = cc.Id;
                        item.CoupleChallengeStatus = cc.Status;
                        item.CurrentProgress = cc.CurrentProgress ?? 0;
                    }
                }
            }

            return new PagedResult<MemberChallengeResponse>
            {
                Items = result,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<MemberChallengeDetailResponse> GetMemberChallengeByIdAsync(int userId, int challengeId)
        {
            // Find member profile
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            // Find couple profile
            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
            if (couple == null)
                throw new Exception("Thành viên chưa thuộc cặp đôi nào");

            // Find challenge
            var challenge = await _unitOfWork.Challenges.GetByIdAsync(challengeId);
            if (challenge == null || (challenge.IsDeleted.HasValue && challenge.IsDeleted != false))
                throw new Exception("Thử thách không tồn tại");

            if (challenge.Status != ChallengeStatus.ACTIVE.ToString())
                throw new Exception("Thử thách chưa khả dụng");

            // Re-use enrich
            var enriched = await EnrichChallengeResponseAsync(new List<Challenge> { challenge });
            var x = enriched.First();

            var response = new MemberChallengeDetailResponse
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                TriggerEvent = x.TriggerEvent,
                RewardPoints = x.RewardPoints,
                GoalMetric = x.GoalMetric,
                TargetGoal = x.TargetGoal,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Status = x.Status,
                RuleData = x.RuleData,
                Instructions = x.Instructions,

                IsJoined = false
            };

            if (couple != null)
            {
                var coupleChallenge = await _unitOfWork.CoupleProfileChallenges.GetByCoupleIdAndChallengeIdAsync(couple.id, challengeId);
                if (coupleChallenge != null)
                {
                    var progressData = JsonConverterUtil.DeserializeOrDefault<CoupleChallengeProgressData>(coupleChallenge.ProgressData);

                    if (progressData?.MemberState != null && progressData.MemberState.TryGetValue(member.Id.ToString(), out var st) && st != null && st.IsJoined)
                    {
                        response.IsJoined = st.IsJoined;
                        response.CoupleChallengeId = coupleChallenge.Id;
                        response.CoupleChallengeStatus = coupleChallenge.Status;
                        response.CurrentProgress = coupleChallenge.CurrentProgress ?? 0;
                        response.JoinedAt = st.JoinedAt;
                    }
                }
            }

            return response;
        }

        public async Task<PagedResult<CoupleChallengeListItemResponse>> GetMyCoupleChallengesAsync(int userId, CoupleChallengeQuery query)
        {
            // Find member profile
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            // Find couple profile
            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
            if (couple == null)
                throw new Exception("Thành viên chưa thuộc cặp đôi nào");

            // Normalize paging
            var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

            // Normalize status
            string? status = null;
            if (query.Status != null)
            {
                status = query.Status.ToString();

                var allowedStatuses = Enum.GetNames(typeof(CoupleProfileChallengeStatus));
                if (!allowedStatuses.Contains(status))
                    throw new Exception($"Trạng thái thử thách của cặp đôi '{status}' không hợp lệ");
            }

            // Normalize q
            var q = string.IsNullOrWhiteSpace(query.Q) ? null : query.Q.Trim();

            // Normalize dates
            var from = query.From;
            var to = query.To;

            // Normalize sort
            var sort = string.IsNullOrWhiteSpace(query.Sort)
                ? "updatedatdesc"
                : query.Sort.Trim().ToLowerInvariant();

            // Create predicate
            Expression<Func<CoupleProfileChallenge, bool>> predicate = cc =>
                cc.CoupleId == couple.id &&
                cc.IsDeleted == false &&
                (status == null || cc.Status == status) &&
                (
                    q == null ||
                    (cc.Challenge != null && (
                           (cc.Challenge.Title != null && cc.Challenge.Title.Contains(q))
                        || (cc.Challenge.Description != null && cc.Challenge.Description.Contains(q))
                    ))
                ) &&
                (from.HasValue == false || (cc.Challenge != null && cc.Challenge.StartDate >= from.Value)) &&
                (to.HasValue == false || (cc.Challenge != null && cc.Challenge.EndDate <= to.Value));

            // Get all raw data
            var allCoupleChallenges = await _unitOfWork.CoupleProfileChallenges.GetAsync(predicate, cc => cc.Include(c => c.Challenge));

            // Get couple challenge and progress into memory
            var memberKey = member.Id.ToString();
            var filtered = allCoupleChallenges
                .Select(cc => new
                {
                    CoupleChallenge = cc,
                    Progress = JsonConverterUtil.DeserializeOrDefault<CoupleChallengeProgressData>(cc.ProgressData)
                })
                .Where(x =>
                    x.Progress?.MemberState != null &&
                    x.Progress.MemberState.TryGetValue(memberKey, out var st) &&
                    st != null &&
                    st.IsJoined
                ).ToList();

            // Apply sorting in memory
            filtered = sort switch
            {
                "updatedatasc" => filtered.OrderBy(x => x.CoupleChallenge.UpdatedAt).ToList(),
                "updatedatdesc" => filtered.OrderByDescending(x => x.CoupleChallenge.UpdatedAt).ToList(),

                "joinedatasc" => filtered.OrderBy(x =>
                    x.Progress!.MemberState![memberKey].JoinedAt ?? DateTime.MaxValue).ToList(),
                "joinedatdesc" => filtered.OrderByDescending(x =>
                    x.Progress!.MemberState![memberKey].JoinedAt ?? DateTime.MinValue).ToList(),

                _ => filtered.OrderByDescending(x => x.CoupleChallenge.UpdatedAt).ToList()
            };

            var totalCount = filtered.Count;
            var paged = filtered
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Map to response
            var filteredEntities = paged.Select(x => x.CoupleChallenge).ToList();
            var challengeResponse = await EnrichChallengeResponseAsync(filteredEntities.Select(cc => cc.Challenge).ToList());
            var challengeMap = challengeResponse.ToDictionary(c => c.Id);
            var response = _mapper.Map<List<CoupleChallengeListItemResponse>>(filteredEntities);

            for (int i = 0; i < paged.Count; i++)
            {
                var item = paged[i];
                var r = response[i];

                if (challengeMap.TryGetValue(r.ChallengeId, out var challenge))
                    r.Challenge = challenge;

                if (item.Progress?.MemberState != null &&
                    item.Progress.MemberState.TryGetValue(memberKey, out var st) &&
                    st != null)
                {
                    r.JoinedAt = st.JoinedAt;
                }

                r.CurrentProgress = item.CoupleChallenge.CurrentProgress ?? 0;
                r.TargetProgress = r.Challenge != null ? r.Challenge.TargetGoal : 0;
                r.ProgressText = ChallengeProgressTextFormatter.Build(
                    r.Challenge.TriggerEvent,
                    r.Challenge.GoalMetric,
                    r.CurrentProgress,
                    r.TargetProgress,
                    item.Progress.IsCompleted,
                    ChallengeProgressExtraBuilder.Build(item.Progress, member.Id)
                );
            }

            return new PagedResult<CoupleChallengeListItemResponse>
            {
                Items = response,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        private DateTime? GetJoinedAt(CoupleProfileChallenge cc, int memberId)
        {
            var key = memberId.ToString();
            var progressData = JsonConverterUtil.DeserializeOrDefault<CoupleChallengeProgressData>(cc.ProgressData);
            if (progressData?.MemberState != null && progressData.MemberState.TryGetValue(key, out var st) && st != null)
            {
                return st.JoinedAt;
            }
            return null;
        }

        public async Task<CoupleChallengeListItemResponse> JoinChallengeAsync(int userId, int challengeId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
            if (couple == null)
                throw new Exception("Thành viên chưa thuộc cặp đôi nào");

            var challenge = await _unitOfWork.Challenges.GetByIdAsync(challengeId);
            if (challenge == null || (challenge.IsDeleted.HasValue && challenge.IsDeleted != false))
                throw new Exception("Thử thách không tồn tại");

            if (challenge.Status != ChallengeStatus.ACTIVE.ToString())
                throw new Exception("Thử thách chưa khả dụng");

            var existing = await _unitOfWork.CoupleProfileChallenges.GetByCoupleIdAndChallengeIdAsync(couple.id, challengeId);
            var coupleChallenge = existing;
            var actorKey = member.Id.ToString();
            if (coupleChallenge != null)
            {
                var existProgress = JsonConverterUtil.DeserializeOrDefault<CoupleChallengeProgressData>(existing.ProgressData);

                EnsureMemberState(existProgress, couple.MemberId1, couple.MemberId2);

                // Member already joined
                if (existProgress.MemberState.TryGetValue(actorKey, out var st) && st.IsJoined)
                    throw new Exception("Bạn đã tham gia thử thách này");

                existProgress.MemberState[actorKey].IsJoined = true;
                existProgress.MemberState[actorKey].JoinedAt = DateTime.UtcNow;
                existProgress.MemberState[actorKey].IsActive = true;
                existProgress.MemberState[actorKey].LeftAt = null;

                existing.ProgressData = JsonConverterUtil.Serialize(existProgress);
                existing.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.CoupleProfileChallenges.Update(existing);
            }
            else
            {
                var items = GetVenueIdsFromChallenge(challenge);

                // Create default progress data
                var coupleChallengeProgress = new CoupleChallengeProgressData
                {
                    Trigger = challenge.TriggerEvent,
                    Metric = challenge.GoalMetric,
                    Target = challenge.TargetGoal.Value,
                    Current = 0,
                    IsCompleted = false,
                    Members = new Dictionary<string, ProgressMember>()
                    {
                        { couple.MemberId1.ToString(), new ProgressMember() },
                        { couple.MemberId2.ToString(), new ProgressMember() }
                    },
                    MemberState = new Dictionary<string, MemberState>()
                    {
                        { couple.MemberId1.ToString(), new MemberState() },
                        { couple.MemberId2.ToString(), new MemberState() }
                    },
                    Unique = challenge.GoalMetric == ChallengeConstants.GoalMetrics.UNIQUE_LIST ? new ProgressUnique
                    {
                        Items = items,
                        ByMember = new Dictionary<string, List<string>>()
                    {
                        { couple.MemberId1.ToString(), new List<string>() },
                        { couple.MemberId2.ToString(), new List<string>() }
                    }
                    } : new(),
                    Streak = challenge.GoalMetric == ChallengeConstants.GoalMetrics.STREAK ? new ProgressStreak
                    {
                        Mode = "DAILY",
                        Current = 0,
                        Best = 0,
                        LastActionAt = null,
                        ByMember = new Dictionary<string, StreakByMember>()
                    {
                        { couple.MemberId1.ToString(), new StreakByMember() },
                        { couple.MemberId2.ToString(), new StreakByMember() }
                    }
                    } : new(),
                    QualifiedItems = challenge.GoalMetric != ChallengeConstants.GoalMetrics.STREAK ? new List<QualifiedProgressItem>() : null,
                    Events = challenge.TriggerEvent != ChallengeTriggerEvent.CHECKIN.ToString() ? new List<ProgressEvent>() : null,
                    DailyHistory = challenge.TriggerEvent == ChallengeTriggerEvent.CHECKIN.ToString() ? new DailyHistory
                    {
                        Months = new Dictionary<string, Dictionary<string, int>>()
                    } : null
                };

                coupleChallengeProgress.MemberState[actorKey].IsJoined = true;
                coupleChallengeProgress.MemberState[actorKey].JoinedAt = DateTime.UtcNow;
                coupleChallengeProgress.MemberState[actorKey].IsActive = true;
                coupleChallengeProgress.MemberState[actorKey].LeftAt = null;

                // Serialize progress data to JSON
                var progressJson = JsonConverterUtil.Serialize(coupleChallengeProgress);

                coupleChallenge = new CoupleProfileChallenge
                {
                    CoupleId = couple.id,
                    ChallengeId = challengeId,
                    CurrentProgress = 0,
                    Status = CoupleProfileChallengeStatus.IN_PROGRESS.ToString(),
                    ProgressData = progressJson,
                };

                await _unitOfWork.CoupleProfileChallenges.AddAsync(coupleChallenge);
            }
            await _unitOfWork.SaveChangesAsync();
            var response = _mapper.Map<CoupleChallengeListItemResponse>(coupleChallenge);
            var challengeResponse = await EnrichChallengeResponseAsync(new List<Challenge> { challenge });
            var challengeMap = challengeResponse.ToDictionary(c => c.Id);

            if (challengeMap.TryGetValue(response.ChallengeId, out var challengeRes))
            {
                response.Challenge = challengeRes;
                response.JoinedAt = GetJoinedAt(coupleChallenge, member.Id);
                response.TargetProgress = challenge.TargetGoal.Value;
                response.ProgressText = ChallengeProgressTextFormatter.Build(challenge.TriggerEvent, challenge.GoalMetric, 0, challenge.TargetGoal.Value, false);
            }

            return response;
        }

        private static void EnsureMemberState(CoupleChallengeProgressData p, int memberId1, int memberId2)
        {
            p.MemberState ??= new Dictionary<string, MemberState>();

            void ensure(int mid)
            {
                var key = mid.ToString();
                if (!p.MemberState.TryGetValue(key, out var st) || st == null)
                    p.MemberState[key] = new MemberState();
            }

            ensure(memberId1);
            ensure(memberId2);
        }

        private List<string> GetVenueIdsFromChallenge(Challenge challenge)
        {
            var allVenueIds = new List<string>();

            if (string.IsNullOrEmpty(challenge.ConditionRules))
                return allVenueIds;

            try
            {
                var ruleWrapper = JsonConverterUtil.DeserializeOrDefault<ChallengeRuleWrapper>(challenge.ConditionRules);
                if (ruleWrapper?.Rules != null)
                {
                    foreach (var rule in ruleWrapper.Rules)
                    {
                        if (rule.Key == ChallengeConstants.RuleKeys.VENUE_ID && rule.Value is JsonElement valElement)
                        {
                            var ids = JsonSerializer.Deserialize<List<string>>(valElement);
                            if (ids != null)
                                allVenueIds.AddRange(ids);
                        }
                    }
                }

                return allVenueIds;
            }
            catch (JsonException)
            {
                throw new Exception("Lỗi khi phân tích điều kiện thử thách");
            }
        }

        public async Task<int> LeaveCoupleChallengeAsync(int userId, int coupleChallengeId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
            if (couple == null)
                throw new Exception("Thành viên chưa thuộc cặp đôi nào");

            var coupleChallenge = await _unitOfWork.CoupleProfileChallenges.GetByIdAsync(coupleChallengeId);
            if (coupleChallenge == null || coupleChallenge.CoupleId != couple.id || coupleChallenge.IsDeleted == true)
                throw new Exception("Thử thách của cặp đôi không tồn tại");

            if (coupleChallenge.ChallengeId == 14)
                throw new Exception("Thử thách 'Check-in mood mỗi ngày' là thử thách đặc biệt, không thể rời khỏi");

            var progressData = JsonConverterUtil.DeserializeOrDefault<CoupleChallengeProgressData>(coupleChallenge.ProgressData);
            if (progressData?.MemberState == null)
                throw new Exception("Dữ liệu tiến độ thử thách không hợp lệ");

            var memberKey = member.Id.ToString();
            if (!progressData.MemberState.TryGetValue(memberKey, out var state) || state == null || !state.IsJoined)
                throw new Exception("Bạn chưa tham gia thử thách này");

            state.IsJoined = false;
            state.IsActive = false;
            state.LeftAt = DateTime.UtcNow;

            coupleChallenge.ProgressData = JsonConverterUtil.Serialize(progressData);
            coupleChallenge.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.CoupleProfileChallenges.Update(coupleChallenge);
            await _unitOfWork.SaveChangesAsync();
            return coupleChallenge.Id;
        }

        public async Task<CoupleChallengeDetailResponse> GetCoupleChallengeProgressAsync(int userId, int coupleChallengeId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
            if (couple == null)
                throw new Exception("Thành viên chưa thuộc cặp đôi nào");

            var coupleChallenge = await _unitOfWork.CoupleProfileChallenges.GetByIdAsync(coupleChallengeId);
            if (coupleChallenge == null || coupleChallenge.CoupleId != couple.id || coupleChallenge.IsDeleted == true)
                throw new Exception("Thử thách của cặp đôi không tồn tại");

            var challenge = await _unitOfWork.Challenges.GetByIdAsync(coupleChallenge.ChallengeId);
            if (challenge == null || (challenge.IsDeleted.HasValue && challenge.IsDeleted != false))
                throw new Exception("Thử thách không tồn tại");

            var progressData = JsonConverterUtil.DeserializeOrDefault<CoupleChallengeProgressData>(coupleChallenge.ProgressData);
            if (progressData?.MemberState == null)
                throw new Exception("Dữ liệu tiến độ thử thách không hợp lệ");

            var memberKey = member.Id.ToString();
            if (!progressData.MemberState.TryGetValue(memberKey, out var state) || state == null || !state.IsJoined)
                throw new Exception("Bạn chưa tham gia thử thách này");

            // Map first
            var response = _mapper.Map<CoupleChallengeDetailResponse>(coupleChallenge);

            // Then enrich
            var challengeEnriched = await EnrichChallengeResponseAsync(new List<Challenge> { challenge });
            response.Challenge = challengeEnriched.First();

            // memberMap
            var coupleMembers = await _unitOfWork.CoupleProfiles.GetCoupleMemberAsync(couple.id);
            var memberNameMap = coupleMembers
                .GroupBy(x => x.Id)
                .ToDictionary(
                    g => g.Key,
                    g => g.First().FullName ?? string.Empty
                );

            // Build extra progress info
            var extra = ChallengeProgressExtraBuilder.BuildDetail(progressData, member.Id, memberNameMap);

            response.JoinedAt = state.JoinedAt;
            response.IsJoined = state.IsJoined;
            response.IsCompleted = progressData.IsCompleted;
            response.TargetProgress = challenge.TargetGoal ?? 0;
            response.ProgressText = ChallengeProgressTextFormatter.Build(
                challenge.TriggerEvent,
                challenge.GoalMetric,
                coupleChallenge.CurrentProgress ?? 0,
                challenge.TargetGoal ?? 0,
                progressData.IsCompleted,
                extra
            );
            response.ProgressExtra = extra;

            // Build members
            response.Members = BuildMemberProgress(
                challenge.TriggerEvent,
                progressData,
                coupleMembers.ToList(),
                member.Id
            );

            return response;
        }

        private static List<CoupleChallengeMemberProgressResponse> BuildMemberProgress(
            string? triggerEvent,
            CoupleChallengeProgressData progressData,
            List<MemberProfile> coupleMembers,
            int currentMemberId)
        {
            var result = new List<CoupleChallengeMemberProgressResponse>();

            foreach (var member in coupleMembers)
            {
                var memberKey = member.Id.ToString();
                progressData.MemberState.TryGetValue(memberKey, out var state);
                progressData.Members.TryGetValue(memberKey, out var memberProgress);

                var item = new CoupleChallengeMemberProgressResponse
                {
                    MemberId = member.Id,
                    MemberName = member.FullName ?? string.Empty,
                    AvatarUrl = member.User != null ? member.User.AvatarUrl : null,
                    IsCurrentUser = member.Id == currentMemberId,

                    IsJoined = state != null && state.IsJoined,
                    JoinedAt = state?.JoinedAt,
                    LeftAt = state?.LeftAt,

                    HasDoneToday = BuildHasDoneToday(triggerEvent, progressData, member.Id),
                    LastActionAt = memberProgress?.LastActionAt,
                    ContributionCount = memberProgress?.Current
                };

                result.Add(item);
            }

            return result
                .OrderByDescending(x => x.IsCurrentUser)
                .ThenByDescending(x => x.IsJoined)
                .ThenBy(x => x.MemberId)
                .ToList();
        }

        private static bool? BuildHasDoneToday(
            string? triggerEvent,
            CoupleChallengeProgressData progressData,
            int memberId)
        {
            var memberKey = memberId.ToString();

            if (progressData.Members == null ||
                !progressData.Members.TryGetValue(memberKey, out var memberProgress) ||
                memberProgress?.LastActionAt == null)
            {
                return false;
            }

            var nowVn = TimezoneUtil.ToVietNamTime(DateTime.UtcNow);
            var today = DateOnly.FromDateTime(nowVn);

            var lastActionVn = TimezoneUtil.ToVietNamTime(memberProgress.LastActionAt.Value);
            return DateOnly.FromDateTime(lastActionVn) == today;
        }

        public async Task<TodayMoodCheckinStatusResponse> CheckTodayCheckinStatusAsync(int userId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
            if (couple == null)
                throw new Exception("Thành viên chưa thuộc cặp đôi nào");

            var nowVn = TimezoneUtil.ToVietNamTime(DateTime.UtcNow);
            var startOfDayVn = nowVn.Date;
            var endOfDayVn = startOfDayVn.AddDays(1);

            var startUtc = DateTimeNormalizeUtil.NormalizeToUtc(startOfDayVn);
            var endUtc = DateTimeNormalizeUtil.NormalizeToUtc(endOfDayVn);

            var checkins = await _unitOfWork.MemberMoodLogs.GetByMemberIdAsync(member.Id);
            var checkin = checkins
                .Where(c => c.CreatedAt >= startUtc && c.CreatedAt < endUtc)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefault();

            return new TodayMoodCheckinStatusResponse
            {
                HasCheckedInToday = checkin != null,
                CheckedInAt = checkin?.CreatedAt
            };
        }

        public async Task HandleCheckinChallengeProgressAsync(int userId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
            if (couple == null)
                //throw new Exception("Thành viên chưa thuộc cặp đôi nào");
                return;

            bool isNew = false;

            var nowUtc = DateTime.UtcNow;
            var nowVn = TimezoneUtil.ToVietNamTime(nowUtc);
            var today = DateOnly.FromDateTime(nowVn);
            var monthKey = $"{today:yyyy-MM}";
            var memberKey = member.Id.ToString();
            var todayDay = today.Day;

            var challenge = await _unitOfWork.Challenges.GetFirstAsync(
                c => c.IsDeleted == false &&
                     c.Status == ChallengeStatus.ACTIVE.ToString() &&
                     c.TriggerEvent == ChallengeTriggerEvent.CHECKIN.ToString() &&
                     (c.StartDate == null || c.StartDate <= nowUtc) &&
                     (c.EndDate == null || c.EndDate >= nowUtc),
                q => q.OrderByDescending(c => c.CreatedAt)
            );

            if (challenge == null)
                return;

            var coupleChallenge = await _unitOfWork.CoupleProfileChallenges.GetFirstAsync(cc =>
                cc.CoupleId == couple.id &&
                cc.ChallengeId == challenge.Id &&
                cc.IsDeleted == false
            );

            if (coupleChallenge == null)
            {
                isNew = true;
                // Lazy create couple challenge if not exist
                var initProgress = CreateInitialCheckinProgressData(couple);
                coupleChallenge = new CoupleProfileChallenge
                {
                    CoupleId = couple.id,
                    ChallengeId = challenge.Id,
                    Status = CoupleProfileChallengeStatus.IN_PROGRESS.ToString(),
                    CurrentProgress = 0,
                    ProgressData = JsonConverterUtil.Serialize(initProgress),
                    CompletedAt = null,
                };

                await _unitOfWork.CoupleProfileChallenges.AddAsync(coupleChallenge);
            }

            if (coupleChallenge.Status == CoupleProfileChallengeStatus.COMPLETED.ToString())
                return;

            // Deserialize progress data
            var progress = string.IsNullOrWhiteSpace(coupleChallenge.ProgressData)
                ? new CoupleChallengeProgressData()
                : JsonConverterUtil.DeserializeOrDefault<CoupleChallengeProgressData>(coupleChallenge.ProgressData) ?? new CoupleChallengeProgressData();


            // Ensure data not null
            progress.DailyHistory ??= new DailyHistory
            {
                Tz = "Asia/Ho_Chi_Minh",
                Months = new Dictionary<string, Dictionary<string, int>>()
            };

            progress.DailyHistory.Months ??= new Dictionary<string, Dictionary<string, int>>();

            progress.MemberState ??= new Dictionary<string, MemberState>();
            progress.Members ??= new Dictionary<string, ProgressMember>();

            if (!progress.MemberState.ContainsKey(memberKey))
            {
                progress.MemberState[memberKey] = new MemberState
                {
                    IsJoined = true,
                    JoinedAt = nowUtc,
                    IsActive = true,
                    LeftAt = null
                };
            }

            if (!progress.Members.ContainsKey(memberKey))
            {
                progress.Members[memberKey] = new ProgressMember
                {
                    Current = 0,
                    Streak = 0,
                    LastActionAt = null
                };
            }

            if (!progress.DailyHistory.Months.TryGetValue(monthKey, out var membermap) || membermap == null)
            {
                membermap = new Dictionary<string, int>();
                progress.DailyHistory.Months[monthKey] = membermap;
            }

            var currentMask = membermap.TryGetValue(memberKey, out var mask) ? mask : 0;

            // If already checked in today, do nothing
            if (CheckinBitMaskUtil.HasCheckin(currentMask, todayDay))
                return;

            // Mark check-in for today
            currentMask = CheckinBitMaskUtil.MarkCheckin(currentMask, todayDay);
            membermap[memberKey] = currentMask;

            // Update member progress
            // MEMBER
            // a. update streak
            var (memberCurrent, memberBest) = CheckinBitMaskUtil.CalculateCrossMonthStreak(progress.DailyHistory.Months, memberKey, null, today);

            var memberStreakObject = progress.Streak.ByMember[memberKey];
            memberStreakObject.Current = memberCurrent;
            memberStreakObject.Best = memberBest;
            memberStreakObject.LastAt = nowUtc;

            // b. update member
            var memberProgressObj = progress.Members[memberKey];
            memberProgressObj.Current += 1;
            memberProgressObj.Streak = memberCurrent;
            memberProgressObj.LastActionAt = nowUtc;

            // COUPLE
            // a. update streak
            var partnerKey = member.Id == couple.MemberId1 ? couple.MemberId2.ToString() : couple.MemberId1.ToString();

            int partnerMask = membermap.TryGetValue(partnerKey, out var pMask) ? pMask : 0;
            int coupleMask = currentMask & partnerMask;

            var (coupleCurrent, coupleBest) = CheckinBitMaskUtil.CalculateCrossMonthStreak(progress.DailyHistory.Months, memberKey, partnerKey, today);

            progress.Streak.Current = coupleCurrent;
            progress.Streak.Best = coupleBest;

            // Goal check
            int memberTodayScore = 1;
            int partnerTodayScore = CheckinBitMaskUtil.HasCheckin(partnerMask, todayDay) ? 1 : 0;

            coupleChallenge.CurrentProgress = memberTodayScore + partnerTodayScore;
            progress.Current = memberTodayScore + partnerTodayScore;
            if (coupleChallenge.CurrentProgress >= challenge.TargetGoal && !progress.IsCompleted)
            {
                coupleChallenge.Status = CoupleProfileChallengeStatus.COMPLETED.ToString();
                coupleChallenge.CompletedAt = nowUtc;
                progress.IsCompleted = true;
            }

            // Add points
            couple.TotalPoints += challenge.RewardPoints;

            // Serialize and save
            coupleChallenge.ProgressData = JsonConverterUtil.Serialize(progress);
            coupleChallenge.UpdatedAt = nowUtc;

            if (!isNew)
                _unitOfWork.CoupleProfileChallenges.Update(coupleChallenge);
        }

        private static CoupleChallengeProgressData CreateInitialCheckinProgressData(CoupleProfile couple)
        {
            var memberIds = new List<int>();

            if (couple.MemberId1 > 0)
                memberIds.Add(couple.MemberId1);

            if (couple.MemberId2 > 0)
                memberIds.Add(couple.MemberId2);

            var progress = new CoupleChallengeProgressData
            {
                DailyHistory = new DailyHistory
                {
                    Tz = "Asia/Ho_Chi_Minh",
                    Months = new Dictionary<string, Dictionary<string, int>>()
                },
                MemberState = new Dictionary<string, MemberState>(),
                Members = new Dictionary<string, ProgressMember>(),
                Streak = new ProgressStreak
                {
                    ByMember = new Dictionary<string, StreakByMember>()
                    {
                        { couple.MemberId1.ToString(), new StreakByMember() },
                        { couple.MemberId2.ToString(), new StreakByMember() }
                    }
                },
                Trigger = ChallengeTriggerEvent.CHECKIN.ToString(),
                Metric = ChallengeConstants.GoalMetrics.STREAK,
                Target = 2
            };

            foreach (var memberId in memberIds)
            {
                var key = memberId.ToString();
                progress.MemberState[key] = new MemberState
                {
                    IsJoined = true,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true,
                    LeftAt = null
                };
                progress.Members[key] = new ProgressMember
                {
                    Current = 0,
                    Streak = 0,
                    LastActionAt = null
                };
            }

            return progress;
        }

        public async Task HandleReviewChallengeProgressAsync(int userId, int reviewId, int? venueId = null, bool hasImage = false)
        {
            // 1. Resolve current member and couple
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
            if (couple == null)
                //throw new Exception("Thành viên chưa thuộc cặp đôi nào");
                return;

            var now = DateTime.UtcNow;

            // 2. Load all in-progress REVIEW challenges of this couple
            var coupleChallenges = await _unitOfWork.CoupleProfileChallenges.GetAsync(cc =>
                cc.IsDeleted == false &&
                cc.CoupleId == couple.id &&
                cc.Status == CoupleProfileChallengeStatus.IN_PROGRESS.ToString() &&
                cc.Challenge != null &&
                cc.Challenge.IsDeleted == false &&
                cc.Challenge.Status == ChallengeStatus.ACTIVE.ToString() &&
                cc.Challenge.TriggerEvent == ChallengeTriggerEvent.REVIEW.ToString() &&
                (cc.Challenge.StartDate == null || cc.Challenge.StartDate <= now) &&
                (cc.Challenge.EndDate == null || cc.Challenge.EndDate >= now), cc => cc.Include(cc => cc.Challenge)
            );

            if (coupleChallenges == null || !coupleChallenges.Any())
                return;

            // 3. Process each challenge
            foreach (var coupleChallenge in coupleChallenges)
            {
                var challenge = coupleChallenge.Challenge;
                var memberKey = member.Id.ToString();
                if (challenge == null)
                    continue;

                // 4. Ensure progress data
                var progress = JsonConverterUtil.DeserializeOrDefault<CoupleChallengeProgressData>(coupleChallenge.ProgressData) ?? new CoupleChallengeProgressData();
                progress = EnsureReviewProgressData(progress, challenge, member.Id, now);

                // 5. Skip if member no longer active or join this challenge
                if (!progress.MemberState.TryGetValue(memberKey, out var state) || state == null || !state.IsJoined || !state.IsActive)
                    continue;

                // 6. Prevent duplicate processing for same review
                if (progress.QualifiedItems?.Any(x => x.ReviewId == reviewId) == true)
                    continue;

                // 7. Check challenge rule for this review action
                if (!IsReviewQualified(challenge.ConditionRules, venueId, hasImage))
                    continue;

                // 8. Update progress
                var updated = ApplyReviewMetricProgress(progress, challenge, member.Id, venueId, reviewId, now);
                // Enrich venue name if needed
                if (venueId.HasValue)
                {
                    var venue = await _unitOfWork.VenueLocations.GetByIdAsync(venueId.Value);
                    var venueName = venue?.Name;

                    if (progress.QualifiedItems != null)
                    {
                        foreach (var item in progress.QualifiedItems)
                        {
                            if (item.VenueId == venueId.Value)
                            {
                                item.VenueName = venueName;
                            }
                        }
                    }
                }

                if (!updated)
                    continue;

                // 9. Check completion
                if (progress.Current >= progress.Target)
                {
                    progress.Current = progress.Target;
                    progress.IsCompleted = true;
                    coupleChallenge.Status = CoupleProfileChallengeStatus.COMPLETED.ToString();
                    coupleChallenge.CompletedAt = now;
                }

                // 10. Save progress
                coupleChallenge.CurrentProgress = progress.Current;
                coupleChallenge.ProgressData = JsonConverterUtil.Serialize(progress);
                coupleChallenge.UpdatedAt = now;

                _unitOfWork.CoupleProfileChallenges.Update(coupleChallenge);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        private CoupleChallengeProgressData EnsureReviewProgressData(CoupleChallengeProgressData progress, Challenge challenge, int memberId, DateTime now)
        {
            var memberKey = memberId.ToString();
            progress.Trigger ??= ChallengeTriggerEvent.REVIEW.ToString();
            progress.Metric ??= challenge.GoalMetric;
            progress.Target = progress.Target <= 0 ? (challenge.TargetGoal ?? 0) : progress.Target;
            progress.Members ??= new Dictionary<string, ProgressMember>();
            progress.MemberState ??= new Dictionary<string, MemberState>();
            progress.Unique ??= new ProgressUnique();
            progress.Unique.Items ??= new List<string>();
            progress.Unique.ByMember ??= new Dictionary<string, List<string>>();
            progress.QualifiedItems ??= new List<QualifiedProgressItem>();

            // ensure member and memberState and member in unique
            if (!progress.Members.ContainsKey(memberKey))
                progress.Members[memberKey] = new ProgressMember
                {
                    Current = 0,
                    Streak = 0,
                    LastActionAt = null
                };

            if (!progress.MemberState.ContainsKey(memberKey))
                progress.MemberState[memberKey] = new MemberState
                {
                    IsJoined = true,
                    JoinedAt = now,
                    IsActive = true,
                    LeftAt = null
                };

            if (!progress.Unique.ByMember.ContainsKey(memberKey))
            {
                progress.Unique.ByMember[memberKey] = new List<string>();
            }

            return progress;
        }

        private bool IsReviewQualified(string? conditionRules, int? venueId, bool hasImage)
        {
            // no rule? => every review is valid
            if (string.IsNullOrWhiteSpace(conditionRules))
                return true;

            var ruleWrapper = JsonConverterUtil.DeserializeOrDefault<ChallengeRuleWrapper>(conditionRules);

            if (ruleWrapper?.Rules == null || !ruleWrapper.Rules.Any())
                return true;

            foreach (var rule in ruleWrapper.Rules)
            {
                switch (rule.Key)
                {
                    case "venue_id":
                        if (!MatchVenueRule(rule.Value, venueId))
                            return false;
                        break;
                    case "has_image":
                        if (!MatchBoolRule(rule.Value, hasImage))
                            return false;
                        break;
                }
            }

            return true;
        }

        private bool MatchVenueRule(object? ruleValue, int? venueId)
        {
            if (ruleValue == null)
                return true;

            if (!venueId.HasValue)
                return false;

            var actualVenueId = venueId.Value.ToString();

            if (ruleValue is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
                    return true;

                if (element.ValueKind == JsonValueKind.String)
                {
                    var raw = element.GetString();
                    if (string.IsNullOrWhiteSpace(raw))
                        return true;

                    return string.Equals(raw.Trim(), actualVenueId, StringComparison.OrdinalIgnoreCase);
                }

                // case: ["15","18"]
                if (element.ValueKind == JsonValueKind.Array)
                {
                    var venueIds = JsonSerializer.Deserialize<List<string>>(element.GetRawText()) ?? new List<string>();

                    return venueIds.Any(x =>
                        !string.IsNullOrWhiteSpace(x) &&
                        string.Equals(x.Trim(), actualVenueId, StringComparison.OrdinalIgnoreCase));
                }

                // fallback nếu lỡ có number
                if (element.ValueKind == JsonValueKind.Number)
                {
                    return element.GetInt32().ToString() == actualVenueId;
                }
            }

            var text = ruleValue.ToString();
            if (string.IsNullOrWhiteSpace(text))
                return true;

            return string.Equals(text.Trim(), actualVenueId, StringComparison.OrdinalIgnoreCase);
        }

        private bool MatchBoolRule(object? ruleValue, bool actualValue)
        {
            if (ruleValue == null)
                return true;

            if (ruleValue is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
                    return true;

                if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
                    return element.GetBoolean() == actualValue;

                if (element.ValueKind == JsonValueKind.String)
                {
                    var raw = element.GetString();
                    if (string.IsNullOrWhiteSpace(raw))
                        return true;

                    if (bool.TryParse(raw, out var parsed))
                        return parsed == actualValue;
                }
            }

            if (ruleValue is bool boolValue)
                return boolValue == actualValue;

            var text = ruleValue.ToString();
            if (string.IsNullOrWhiteSpace(text))
                return true;

            return bool.TryParse(text, out var value) && value == actualValue;
        }

        private bool ApplyReviewMetricProgress(
            CoupleChallengeProgressData progress,
            Challenge challenge,
            int memberId,
            int? venueId,
            int reviewId,
            DateTime now)
        {
            var memberKey = memberId.ToString();
            if (challenge.GoalMetric == ChallengeConstants.GoalMetrics.COUNT)
            {
                // handle count review
                progress.Current += 1;
                progress.Members[memberKey].Current += 1;
                progress.Members[memberKey].LastActionAt = now;
            }
            if (challenge.GoalMetric == ChallengeConstants.GoalMetrics.UNIQUE_LIST)
            {
                // handle unique venue Id review
                if (!venueId.HasValue)
                    return false;

                var uniqueKey = $"venueId:{venueId.Value}";

                if (progress.Unique.Items.Contains(uniqueKey))
                    return false;

                progress.Unique.Items.Add(uniqueKey);
                progress.Unique.ByMember[memberKey].Add(uniqueKey);

                progress.Current = progress.Unique.Items.Count;
                progress.Members[memberKey].Current = progress.Unique.ByMember[memberKey].Count;
                progress.Members[memberKey].LastActionAt = now;
            }

            progress.QualifiedItems.Add(new QualifiedProgressItem
            {
                ReviewId = reviewId,
                VenueId = venueId,
                VenueName = null,
                ActionAt = now,
                MemberId = memberId,
                Type = progress.Trigger
            });

            return true;
        }
    }
}
