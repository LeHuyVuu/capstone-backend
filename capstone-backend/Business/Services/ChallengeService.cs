using AutoMapper;
using capstone_backend.Business.Common.Constants;
using capstone_backend.Business.DTOs.Challenge;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Extensions.Common;
using CsvHelper;
using System.Diagnostics;
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
                                var intList = JsonSerializer.Deserialize<List<int>>(element.GetRawText());
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
                                    rawValue = new List<int>();
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
            var allVenueIds = new List<int>();

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
                                var ids = JsonSerializer.Deserialize<List<int>>(valElement);
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
            var venueIdToName = venueLookup.ToDictionary(v => v.Id, v => v.Name);

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
                                    var ids = JsonSerializer.Deserialize<List<int>>(valElement);
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
    }
}
