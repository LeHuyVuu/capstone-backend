using capstone_backend.Api.Models;
using capstone_backend.Business.DTOs.Recommendation;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace capstone_backend.Api.Controllers;

/// <summary>
/// Controller for AI-powered venue recommendations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RecommendationController : BaseController
{
    private readonly IRecommendationService _recommendationService;
    private readonly ILogger<RecommendationController> _logger;

    public RecommendationController(
        IRecommendationService recommendationService,
        ILogger<RecommendationController> logger)
    {
        _recommendationService = recommendationService;
        _logger = logger;
    }

    /// <summary>
    /// 🤖 AI-Powered Venue Recommendation Engine - Ultra Flexible Input
    /// </summary>
    [HttpPost]
    [SwaggerOperation(
        Summary = "🤖 AI-Powered Venue Recommendations",
        Description = @"Ultra-flexible recommendation engine - accepts natural language, structured data, geo-location, or any combination. AI analyzes MBTI, mood, location, preferences to suggest perfect venues.

## 🎯 API này hỗ trợ NHIỀU cách truyền input:

---

### 📝 **Case 1: Natural Language Query (Ngôn ngữ tự nhiên)**
AI tự động parse query để hiểu ý định, tâm trạng, preferences
```json
{
  ""query"": ""Hôm nay anniversary, muốn đi đâu đó lãng mạn ở Hà Nội""
}
```

---

### 📊 **Case 2: Structured Data Only (Chỉ dữ liệu có cấu trúc)**
Truyền MBTI, mood, region
```json
{
  ""mbtiType"": ""INTJ"",
  ""moodId"": 1,
  ""region"": ""Hà Nội"",
  ""limit"": 10
}
```

---

### 🎭 **Case 3: Couple Recommendation (Gợi ý cho cặp đôi)**
Truyền MBTI và mood của cả 2 người
```json
{
  ""query"": ""Muốn đi date cuối tuần"",
  ""mbtiType"": ""INFP"",
  ""partnerMbtiType"": ""ESFJ"",
  ""moodId"": 2,
  ""partnerMoodId"": 3,
  ""region"": ""Hồ Chí Minh""
}
```

---

### 📍 **Case 4: Geo-Location Filtering - Region String (Lọc theo khu vực)**
Sử dụng region string - AI tự động map sang bounding box chính xác
```json
{
  ""query"": ""Muốn đi cafe yên tĩnh"",
  ""region"": ""Hà Nội"",
  ""limit"": 10
}
```
**Supported Regions**: Hà Nội, Hồ Chí Minh, Đà Nẵng, Hải Phòng, Cần Thơ, Nha Trang, Huế, Vũng Tàu, Đà Lạt, Phú Quốc

---

### 🌐 **Case 5: Geo-Location Filtering - Latitude/Longitude (Tọa độ GPS chính xác)**
Truyền lat/lon để lọc theo bán kính (CHÍNH XÁC NHẤT)
```json
{
  ""latitude"": 21.028511,
  ""longitude"": 105.804817,
  ""radiusKm"": 5,
  ""query"": ""Cafe gần đây""
}
```
- `radiusKm` mặc định = 5km nếu không truyền
- Sử dụng tọa độ GPS của user để lọc địa điểm trong bán kính

---

### 🗺️ **Case 6: Hybrid Location Filtering (Kết hợp cả region và lat/lon)**
Nếu truyền cả 2, **lat/lon sẽ được ưu tiên** (chính xác hơn)
```json
{
  ""query"": ""Nhà hàng sang trọng gần đây"",
  ""region"": ""Hà Nội"",
  ""latitude"": 21.028511,
  ""longitude"": 105.804817,
  ""radiusKm"": 3
}
```
→ Hệ thống sẽ dùng lat/lon thay vì region string

---

### 💰 **Case 7: With Budget Filter (Có lọc theo ngân sách)**
```json
{
  ""query"": ""Muốn đi ăn tối sang trọng"",
  ""region"": ""Đà Nẵng"",
  ""budgetLevel"": 3,
  ""limit"": 5
}
```
**Budget Levels**: 1 = Thấp (< 200k), 2 = Trung bình (200k-500k), 3 = Cao (> 500k)

---

### 🎨 **Case 8: Mixed Input (Kết hợp tự do mọi field)**
Truyền cả query tự nhiên + structured data + geo-location
```json
{
  ""query"": ""Muốn đi cafe yên tĩnh để làm việc"",
  ""mbtiType"": ""INTJ"",
  ""latitude"": 10.762622,
  ""longitude"": 106.660172,
  ""radiusKm"": 2,
  ""budgetLevel"": 2,
  ""limit"": 8
}
```

---

### 🌍 **Case 9: Minimal Input (Tối thiểu - chỉ location)**
Chỉ cần region hoặc lat/lon, AI sẽ suggest địa điểm phổ biến
```json
{
  ""region"": ""Đà Nẵng""
}
```
HOẶC
```json
{
  ""latitude"": 16.047079,
  ""longitude"": 108.206230,
  ""radiusKm"": 10
}
```

---

### 🎪 **Case 10: Special Events/Occasions (Sự kiện đặc biệt)**
```json
{
  ""query"": ""Birthday party cho 10 người, không gian rộng rãi, có karaoke"",
  ""region"": ""Hà Nội"",
  ""budgetLevel"": 3
}
```

---

### 😊 **Case 11: Mood-Based Only (Chỉ dựa vào tâm trạng)**
```json
{
  ""moodId"": 5,
  ""latitude"": 21.028511,
  ""longitude"": 105.804817,
  ""radiusKm"": 3,
  ""limit"": 15
}
```

---

### 🧠 **Case 12: MBTI Personality Match (Personality của cặp đôi)**
```json
{
  ""mbtiType"": ""ENFP"",
  ""partnerMbtiType"": ""ISTJ"",
  ""region"": ""Hà Nội""
}
```
AI sẽ tìm venue phù hợp cho cả 2 personality types

---

### 🗣️ **Case 13: Complex Vietnamese Query (Query phức tạp)**
```json
{
  ""query"": ""Tối nay muốn đi ăn đồ Nhật, không gian sang trọng nhưng không quá đông, view đẹp thì tốt, gần Hồ Tây""
}
```

---

### 🎯 **Case 14: Near My Location (Gần vị trí hiện tại)**
```json
{
  ""query"": ""Quán ăn ngon gần đây"",
  ""latitude"": 10.762622,
  ""longitude"": 106.660172,
  ""radiusKm"": 1
}
```

---

### 🌆 **Case 15: No Location Filter (Không lọc theo địa điểm)**
Không truyền region/lat/lon → Search toàn quốc
```json
{
  ""query"": ""Resort view biển đẹp nhất Việt Nam"",
  ""budgetLevel"": 3,
  ""limit"": 20
}
```

---

## 📋 **Request Fields (TẤT CẢ đều NULLABLE)**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `query` | string | ❌ | Natural language query (Tiếng Việt/English) |
| `mbtiType` | string | ❌ | MBTI của user (INTJ, ENFP, ISTJ, etc.) |
| `partnerMbtiType` | string | ❌ | MBTI của partner (cho couple recommendation) |
| `moodId` | int | ❌ | ID tâm trạng của user (1-10) |
| `partnerMoodId` | int | ❌ | ID tâm trạng của partner |
| `region` | string | ❌ | Tên khu vực (Hà Nội, Hồ Chí Minh, Đà Nẵng...) |
| `latitude` | decimal | ❌ | Latitude GPS (-90 to 90) |
| `longitude` | decimal | ❌ | Longitude GPS (-180 to 180) |
| `radiusKm` | decimal | ❌ | Bán kính tìm kiếm (default = 5km) |
| `budgetLevel` | int | ❌ | Mức ngân sách (1=Thấp, 2=Trung, 3=Cao) |
| `limit` | int | ✅ | Số lượng kết quả (default=10, max=20) |

---

## 💡 **Important Tips**:

### Location Filtering (3 modes):
1. **Latitude + Longitude** (PRIORITY 1 - Chính xác nhất)
   - Nếu truyền lat/lon → Hệ thống dùng bounding box radius search
   - Region string sẽ bị IGNORE nếu có lat/lon
   
2. **Region String** (PRIORITY 2 - Dùng khi không có GPS)
   - Hệ thống tự map region → Bounding box của thành phố
   - Hỗ trợ 10 thành phố lớn tại Việt Nam
   
3. **No Location Filter** (Không lọc)
   - Search toàn bộ database
   - Kết quả sắp xếp theo match score

### General Tips:
- **Không cần truyền đủ tất cả field** - AI làm việc với bất kỳ thông tin nào
- **Query càng chi tiết**, recommendation càng chính xác
- **Lat/Lon chính xác hơn Region string** (ưu tiên dùng nếu có GPS)
- **RadiusKm** mặc định 5km, có thể tùy chỉnh (1-50km)
- AI hiểu cả **Tiếng Việt** và **English**
- Response time: ~1-2s cho most cases

---

## ⚡ **Response Format**:
```json
{
  ""success"": true,
  ""message"": ""Successfully generated 20 recommendations in 5871ms"",
  ""code"": 200,
  ""data"": {
    ""recommendations"": [
      {
        ""venueLocationId"": 1,
        ""name"": ""Cà phê Bên Sông Hàn"",
        ""address"": ""12 Bạch Đằng, Hải Châu, Đà Nẵng"",
        ""description"": ""Quán cà phê view sông, phù hợp đi dạo tối và trò chuyện."",
        ""matchReason"": ""Phù hợp với sở thích của bạn"",
        ""averageRating"": 5,
        ""reviewCount"": 1,
        ""coverImage"": null,
        ""interiorImage"": null,
        ""fullPageMenuImage"": null,
        ""matchedTags"": [
          ""CẢ HAI YÊN TĨNH"",
          ""LÃNG MẠN""
        ]
      },
      {
        ""venueLocationId"": 3,
        ""name"": ""Gốm & Trà Thảo Điền"",
        ""address"": ""25 Xuân Thủy, Thảo Điền, Thủ Đức, TP.HCM"",
        ""description"": ""Workshop gốm + trà, trải nghiệm mới, an toàn, dễ gắn kết."",
        ""matchReason"": ""Phù hợp với sở thích của bạn"",
        ""averageRating"": 5,
        ""reviewCount"": 1,
        ""coverImage"": null,
        ""interiorImage"": null,
        ""fullPageMenuImage"": null,
        ""matchedTags"": [
          ""HỨNG THÚ KHÁM PHÁ"",
          ""VUI VẺ""
        ]
      }
    ],
    ""explanation"": ""Dựa trên phân tích của chúng tôi, đây là những địa điểm phù hợp nhất cho bạn."",
    ""coupleMoodType"": null,
    ""personalityTags"": [],
    ""processingTimeMs"": 5871
  },
  ""traceId"": ""0HNITO4TEGVTE:00000001"",
  ""timestamp"": ""2026-01-27T16:29:30.406611Z""
}
```

---

## 🎯 **Response Fields**:

| Field | Type | Description |
|-------|------|-------------|
| `venueLocationId` | int | ID của địa điểm |
| `name` | string | Tên địa điểm |
| `address` | string | Địa chỉ |
| `description` | string | Mô tả ngắn |
| `matchReason` | string | Lý do AI recommend |
| `averageRating` | decimal? | Rating trung bình (null nếu không có review) |
| `reviewCount` | int | Số review |
| `coverImage` | string? | Ảnh bìa (null nếu chưa có) |
| `interiorImage` | string? | Ảnh nội thất (null nếu chưa có) |
| `fullPageMenuImage` | string? | Ảnh menu (null nếu chưa có) |
| `matchedTags` | array | Tags match (mood/personality) |
| `explanation` | string | Giải thích tổng thể từ AI |
| `coupleMoodType` | string? | Tâm trạng cặp đôi detected (null nếu không áp dụng) |
| `personalityTags` | array | Personality tags detected (empty nếu không có) |
| `processingTimeMs` | long | Thời gian xử lý (ms) |
| `traceId` | string | Correlation ID cho debugging |
| `timestamp` | string | Timestamp khi response được tạo (ISO 8601) |",
        OperationId = "GetRecommendations",
        Tags = new[] { "Recommendation" }
    )]
    [SwaggerResponse(200, "Successfully generated personalized recommendations", typeof(ApiResponse<RecommendationResponse>))]
    [SwaggerResponse(400, "Invalid request parameters", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    [ProducesResponseType(typeof(ApiResponse<RecommendationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRecommendations([FromBody] RecommendationRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Recommendation request - Query: {Query}, MBTI: {Mbti}, Partner: {Partner}, Mood: {Mood}, Region: {Region}",
                request.Query, request.MbtiType, request.PartnerMbtiType, request.MoodId, request.Region);

            var result = await _recommendationService.GetRecommendationsAsync(request);

            var message = result.Recommendations.Any()
                ? $"Successfully generated {result.Recommendations.Count} recommendations in {result.ProcessingTimeMs}ms"
                : "No venues found matching your criteria, but here are some general suggestions";

            return OkResponse(result, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating recommendations for request: {@Request}", request);

            return InternalServerErrorResponse(
                "An error occurred while generating recommendations. Please try again later.");
        }
    }
}
