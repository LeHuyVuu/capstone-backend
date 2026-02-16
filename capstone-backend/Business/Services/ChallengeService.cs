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

        public async Task<object> CreateChallengeAsyncV2(int userId, CreateChallengeRequest request)
        {
            Validate(request);

            var allowedKeys = new Dictionary<string, List<string>>
            {
                { ChallengeTriggerEvent.REVIEW.ToString(), new List<string> { ChallengeConstants.RuleKeys.VENUE_ID, ChallengeConstants.RuleKeys.HAS_IMAGE } },
                { ChallengeTriggerEvent.POST.ToString(), new List<string> { ChallengeConstants.RuleKeys.HASH_TAG, ChallengeConstants.RuleKeys.HAS_IMAGE } },
                { ChallengeTriggerEvent.CHECKIN.ToString(), new List<string> { ChallengeConstants.RuleKeys.VENUE_ID } }
            };

            var ruleList = new List<object>();
            var whiteList = allowedKeys.GetValueOrDefault(request.TriggerEvent) ?? new List<string>();

            if (request.RuleData != null)
            {
                foreach (var item in request.RuleData)
                {
                    if (item.Value == null)
                        continue;

                    if (!whiteList.Contains(item.Key)) 
                        throw new Exception($"Key '{item.Key}' không được cho phép trong event '{request.TriggerEvent}'.");

                    string key = item.Key;
                    object rawValue = item.Value;
                    string op = ChallengeConstants.RuleOps.Eq;

                    if (rawValue is JsonElement element)
                    {
                        switch (element.ValueKind)
                        {
                            case JsonValueKind.Array:
                                op = ChallengeConstants.RuleOps.In;
                                var list = JsonSerializer.Deserialize<List<object>>(element.GetRawText());
                                rawValue = list;

                                if (list != null)
                                {
                                    if (key == ChallengeConstants.RuleKeys.VENUE_ID)
                                    {
                                        list = list.Select(x => x).Distinct().Cast<object>().ToList();
                                    }

                                    rawValue = list;

                                    // Cập nhật TargetGoal dựa trên danh sách ĐÃ LỌC TRÙNG
                                    if (request.GoalMetric == ChallengeConstants.GoalMetrics.UNIQUE_LIST)
                                        request.TargetGoal = list.Count;
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
            }

            // Convert start and end date to UTC
            if (request.StartDate.HasValue)
                request.StartDate = DateTimeNormalizeUtil.NormalizeToUtc(request.StartDate.Value);
            if (request.EndDate.HasValue)
                request.EndDate = DateTimeNormalizeUtil.NormalizeToUtc(request.EndDate.Value);

            var challenge = _mapper.Map<Challenge>(request);
            challenge.ConditionRules = JsonSerializer.Serialize(new
            {
                logic = "AND",
                rules = ruleList
            });
            challenge.Status = ChallengeStatus.INACTIVE.ToString();

            await _unitOfWork.Challenges.AddAsync(challenge);
            await _unitOfWork.SaveChangesAsync();

            return new { ChallengeId = challenge.Id, Rules = ruleList };
        }

        public async Task<PagedResult<ChallengeResponse>> GetAllChallengesAsync(int pageNumber, int pageSize)
        {
            var challenges = await _unitOfWork.Challenges.GetPagedAsync(
                    pageNumber,
                    pageSize,
                    c => c.IsDeleted == false,
                    c => c.OrderByDescending(ch => ch.CreatedAt)
                );

            // Map to response DTO
            var challengeResponses = _mapper.Map<List<ChallengeResponse>>(challenges.Items);
            var allVenueIds = new List<int>();

            foreach (var challenge in challenges.Items)
            {
                if (string.IsNullOrEmpty(challenge.ConditionRules))
                    continue;

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
            // Get venue names
            var venueLookup = await _unitOfWork.VenueLocations.GetNamesByIdsAsync(allVenueIds);
            var venueIdToName = venueLookup.ToDictionary(v => v.Id, v => v.Name);

            var items = challenges.Items.ToList();

            // Turn rules to dto foreach challenge
            for (int i = 0; i < items.Count; i++)
            {
                var rawRules = items[i].ConditionRules;
                var targetDto = challengeResponses[i];

                var ruleWrapper = JsonSerializer.Deserialize<ChallengeRuleWrapper>(rawRules);
                if (ruleWrapper?.Rules != null)
                {
                    foreach (var r in ruleWrapper.Rules)
                    {
                        var displayRule = new ChallengeRuleDisplayDto
                        {
                            Key = r.Key,
                            RawValue = r.Value
                        };

                        // Translate label
                        displayRule.Label = r.Key switch
                        {
                            ChallengeConstants.RuleKeys.VENUE_ID => "Địa điểm áp dụng",
                            ChallengeConstants.RuleKeys.HAS_IMAGE => "Yêu cầu hình ảnh",
                            ChallengeConstants.RuleKeys.HASH_TAG => "Hashtag bắt buộc",
                            _ => r.Key
                        };

                        // Translate values and operators
                        if (r.Key == ChallengeConstants.RuleKeys.VENUE_ID)
                        {
                            displayRule.Operator = "Danh sách";

                            if (r.Value is JsonElement valElement && valElement.ValueKind == JsonValueKind.Array)
                            {
                                var ids = JsonSerializer.Deserialize<List<int>>(valElement);
                                var names = ids?.Select(id => venueIdToName.ContainsKey(id) ? venueIdToName[id] : id.ToString());
                                displayRule.DisplayValue = names != null ? string.Join(", ", names) : "";
                            }
                        }
                        else if (r.Key == ChallengeConstants.RuleKeys.HAS_IMAGE)
                        {
                            displayRule.Operator = "Là";
                            displayRule.DisplayValue = "Bắt buộc";
                        }
                        else
                        {
                            displayRule.Operator = r.Op == ChallengeConstants.RuleOps.Eq ? "Bằng" : r.Op;
                            displayRule.DisplayValue = r.Value.ToString();
                        }
                    
                        targetDto.Rules.Add(displayRule);
                    }
                }
            }

            return new PagedResult<ChallengeResponse>
            {
                Items = challengeResponses,
                TotalCount = challenges.TotalCount,
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

            if (request.StartDate >= request.EndDate)
                throw new Exception("Ngày bắt đầu phải nhỏ hơn ngày kết thúc");
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
