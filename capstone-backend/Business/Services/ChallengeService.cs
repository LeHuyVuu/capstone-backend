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

            if (request.GoalMetric != ChallengeConstants.GoalMetrics.UNIQUE_LIST && request.TargetGoal <= 0)
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
                            else if (r.Key == ChallengeConstants.RuleKeys.HAS_IMAGE)
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

                IsJoined = false
            }).ToList();

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
    }
}
