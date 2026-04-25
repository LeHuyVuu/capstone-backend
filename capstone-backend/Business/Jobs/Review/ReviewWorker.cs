
using capstone_backend.Business.Common;
using capstone_backend.Business.DTOs.Notification;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;
using System.Numerics;
using System.Text.Json;

namespace capstone_backend.Business.Jobs.Review
{
    public class ReviewWorker : IReviewWorker
    {
        private readonly INotificationService _notificationService;
        private readonly IFcmService? _fcmService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ReviewWorker> _logger;
        private readonly Lazy<ChatClient> _chatClientLazy;

        public ReviewWorker(INotificationService notificationService, IServiceProvider serviceProvider, IUnitOfWork unitOfWork, ILogger<ReviewWorker> logger)
        {
            _notificationService = notificationService;
            _fcmService = serviceProvider.GetService<IFcmService>();
            _unitOfWork = unitOfWork;
            _logger = logger;
            _chatClientLazy = new Lazy<ChatClient>(() =>
            {
                var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                var modelName = Environment.GetEnvironmentVariable("MODEL_NAME") ?? "gpt-4o-mini";

                if (string.IsNullOrWhiteSpace(apiKey))
                    throw new InvalidOperationException("Thiếu OpenAI API Key!");

                return new ChatClient(model: modelName, apiKey: apiKey);
            });
        }

        public async Task EvaluateReviewRelevanceAsync(int reviewId)
        {
            try
            {
                var review = await _unitOfWork.Context.Reviews
                    .Include(r => r.Venue)
                        .ThenInclude(v => v.VenueLocationCategories)
                            .ThenInclude(vc => vc.Category)
                    .Include(r => r.Venue)
                        .ThenInclude(v => v.VenueLocationTags)
                            .ThenInclude(vt => vt.LocationTag)
                                .ThenInclude(t => t.CoupleMoodType)
                    .Include(r => r.Venue)
                        .ThenInclude(v => v.VenueLocationTags)
                            .ThenInclude(vt => vt.LocationTag)
                                .ThenInclude(t => t.CouplePersonalityType)
                    .FirstOrDefaultAsync(r => r.Id == reviewId && r.IsDeleted == false);

                if (review == null)
                    return;

                if (_chatClientLazy == null || string.IsNullOrWhiteSpace(review.Content) || review.Venue == null)
                {
                    review.IsRelevant = false;
                    review.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Reviews.Update(review);
                    await _unitOfWork.SaveChangesAsync();
                    return;
                }

                var venue = review.Venue;
                var categoryNames = venue.VenueLocationCategories?
                    .Where(x => !x.IsDeleted)
                    .Select(x => x.Category?.Name)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList() ?? new List<string>();

                var moodTags = venue.VenueLocationTags?
                    .Where(x => x.IsDeleted != true && x.LocationTag?.CoupleMoodType?.Name != null)
                    .Select(x => x.LocationTag!.CoupleMoodType!.Name)
                    .Distinct()
                    .ToList() ?? new List<string>();

                var personalityTags = venue.VenueLocationTags?
                    .Where(x => x.IsDeleted != true && x.LocationTag?.CouplePersonalityType?.Name != null)
                    .Select(x => x.LocationTag!.CouplePersonalityType!.Name!)
                    .Distinct()
                    .ToList() ?? new List<string>();

                var detailTags = venue.VenueLocationTags?
                    .Where(x => x.IsDeleted != true && x.LocationTag?.DetailTag != null)
                    .SelectMany(x => x.LocationTag!.DetailTag!)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList() ?? new List<string>();

                var systemPrompt = """
Bạn là bộ phân loại relevance cho review địa điểm.
Nhiệm vụ:
- Dựa vào nội dung review và thông tin venue, xác định review có liên quan trực tiếp đến venue hay không.
- Trả về JSON DUY NHẤT theo format:
{"isRelevant": true}
hoặc
{"isRelevant": false}

Quy tắc:
- true: nội dung nói về trải nghiệm tại địa điểm (món ăn, đồ uống, dịch vụ, không gian, giá, nhân viên, chờ bàn, vị trí, tiện ích...).
- false: nội dung spam, quảng cáo không liên quan, nói chuyện ngoài lề không liên quan venue.
""";

                var userPrompt = $"""
Review content:
{review.Content}

Venue info:
- Name: {venue.Name}
- Description: {venue.Description}
- Address: {venue.Address}
- Area: {venue.Area}
- Category: {string.Join(", ", categoryNames)}
- Mood tags: {string.Join(", ", moodTags)}
- Personality tags: {string.Join(", ", personalityTags)}
- Detail tags: {string.Join(", ", detailTags)}
""";

                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(userPrompt)
                };

                using var cts = new CancellationTokenSource(5000);
                var completion = await _chatClientLazy.Value.CompleteChatAsync(messages, cancellationToken: cts.Token);
                var responseText = completion.Value.Content[0].Text?.Trim() ?? string.Empty;

                bool isRelevant = false;

                try
                {
                    using var doc = JsonDocument.Parse(responseText);
                    if (doc.RootElement.TryGetProperty("isRelevant", out var p))
                    {
                        if (p.ValueKind == JsonValueKind.True || p.ValueKind == JsonValueKind.False)
                            isRelevant = p.GetBoolean();
                        else if (p.ValueKind == JsonValueKind.String && bool.TryParse(p.GetString(), out var parsed))
                            isRelevant = parsed;
                    }
                }
                catch
                {
                    if (responseText.Contains("true", StringComparison.OrdinalIgnoreCase))
                        isRelevant = true;
                }

                review.IsRelevant = isRelevant;
                review.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Reviews.Update(review);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REVIEW WORKER] Failed to evaluate relevance for reviewId={ReviewId}", reviewId);
            }
        }

        public async Task RecountReviewAsync(int venueId)
        {
            var venue = await _unitOfWork.VenueLocations.GetByIdAsync(venueId);
            if (venue == null)
                return;

            var reviews = await _unitOfWork.Context.Reviews
                .Where(r => r.VenueId == venueId && r.IsDeleted == false)
                .ToListAsync();

            var reviewCount = reviews.Count;
            var averageRating = reviews.Any()
                ? reviews.Average(r => (decimal)r.Rating)
                : 0m;

            venue.ReviewCount = reviewCount;
            venue.AverageRating = averageRating;

            _unitOfWork.VenueLocations.Update(venue);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task SendReviewNotificationAsync(int checkInHistoryId)
        {
            var checkInHistory = await _unitOfWork.CheckInHistories.GetByIdAsync(checkInHistoryId);

            if (checkInHistory == null)
            {
                _logger.LogWarning("Check-in history with ID {CheckInHistoryId} not found.", checkInHistoryId);
                return;
            }

            var member = await _unitOfWork.MembersProfile.GetByIdAsync(checkInHistory.MemberId);
            if (member == null)
            {
                _logger.LogWarning("Member with ID {MemberId} not found.", checkInHistory.MemberId);
                return;
            }

            var venue = await _unitOfWork.VenueLocations.GetByIdAsync(checkInHistory.VenueId);
            if (venue == null)
            {
                _logger.LogWarning("Venue with ID {VenueId} not found.", checkInHistory.VenueId);
                return;
            }

            if (checkInHistory.IsValid != null)
            {
                _logger.LogInformation("Check-in history with ID {CheckInHistoryId} has already been processed. IsValid: {IsValid}", checkInHistoryId, checkInHistory.IsValid);
                return;
            }

            checkInHistory.IsValid = false;
            _unitOfWork.CheckInHistories.Update(checkInHistory);
            await _unitOfWork.SaveChangesAsync();

            // Send notification logic goes here
            await _notificationService.SendNotificationAsync(
                member.UserId,
                new NotificationRequest
                {
                    Title = NotificationTemplate.Review.TitleReviewRequest,
                    Message = NotificationTemplate.Review.GetReviewRequestBody(venue.Name),
                    Type = NotificationType.LOCATION.ToString(),
                    ReferenceId = checkInHistoryId,
                    ReferenceType = ReferenceType.CHECK_IN_HISTORY.ToString(),
                    Data = new Dictionary<string, string>
                    {
                        { NotificationKeys.RefId, venue.Id.ToString() },
                        { NotificationKeys.RefType, ReferenceType.VENUE_LOCATION.ToString() }
                    }
                }
            );

            // Send Push Notification
            if (_fcmService != null)
            {
                var tokens = await _unitOfWork.DeviceTokens.GetTokensByUserId(member.UserId);
                if (tokens == null || !tokens.Any())
                {
                    _logger.LogInformation("No device tokens found for user ID {UserId}.", member.UserId);
                    return;
                }

                await _fcmService.SendMultiNotificationAsync(
                    tokens,
                    new SendNotificationRequest
                    {
                        Title = NotificationTemplate.Review.TitleReviewRequest,
                        Body = NotificationTemplate.Review.GetReviewRequestBody(venue.Name),
                        Data = new Dictionary<string, string>
                        {
                            { NotificationKeys.Type, NotificationType.LOCATION.ToString() },
                            { NotificationKeys.RefId, checkInHistoryId.ToString() },
                            { NotificationKeys.RefType, ReferenceType.CHECK_IN_HISTORY.ToString() },
                            { "venueLocationId", venue.Id.ToString() }
                        }
                    }
                );
            }
        }
    }
}
