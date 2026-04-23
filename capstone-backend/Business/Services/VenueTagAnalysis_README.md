# Venue Tag Analysis System

Hệ thống phân tích độ chính xác của venue tags dựa trên feedback từ member reviews.

## Mục đích

Giúp venue owner biết được tags nào của venue đang phù hợp/không phù hợp với thực tế, dựa trên `CoupleMoodSnapshot` và `IsMatched` trong reviews.

## Cách hoạt động

### 1. Dữ liệu phân tích

```
Venue có tags: ["Yên tĩnh", "Thư thái", "Lãng mạn"]

Reviews:
- Review 1: CoupleMoodSnapshot = "Thư thái,Yên tĩnh", IsMatched = true
- Review 2: CoupleMoodSnapshot = "Lãng mạn,Vui vẻ", IsMatched = false
- Review 3: CoupleMoodSnapshot = "Lãng mạn,Khám phá", IsMatched = false
- Review 4: CoupleMoodSnapshot = "Yên tĩnh,Thư thái", IsMatched = true
```

### 2. Phân tích từng tag

**Tag "Yên tĩnh":**
- Có 2 reviews từ khách có mood "Yên tĩnh"
- 2 matched, 0 unmatched
- Match rate: 100% → ✅ GOOD

**Tag "Thư thái":**
- Có 2 reviews từ khách có mood "Thư thái"
- 2 matched, 0 unmatched
- Match rate: 100% → ✅ GOOD

**Tag "Lãng mạn":**
- Có 2 reviews từ khách có mood "Lãng mạn"
- 0 matched, 2 unmatched
- Match rate: 0% → 🚨 POOR (nên xóa tag này!)

### 3. Ngưỡng đánh giá (có thể điều chỉnh bởi Admin)

```
Match Rate >= 70%: GOOD (✅ Tag phù hợp)
Match Rate 50-69%: WARNING (⚠️ Cần xem xét)
Match Rate < 50%: POOR (🚨 Nên xóa tag)

Minimum reviews: >= 3 reviews có tag đó mới đánh giá
```

## API Endpoints

### 1. Venue Owner - Xem phân tích tags của venue mình

```http
GET /api/venue-owner/tag-analysis/{venueId}
Authorization: Bearer {token}
Role: VENUE_OWNER
```

**Response:**
```json
{
  "success": true,
  "data": {
    "venueId": 123,
    "venueName": "Cafe ABC",
    "overallMatchRate": 66.7,
    "totalReviews": 10,
    "tagAnalysis": [
      {
        "tag": "Yên tĩnh",
        "tagType": "CoupleMoodType",
        "status": "GOOD",
        "severity": "NONE",
        "totalReviews": 5,
        "matchedCount": 4,
        "unmatchedCount": 1,
        "matchRate": 80.0,
        "message": "Tag 'Yên tĩnh' phù hợp với venue (80.0% khách hàng hài lòng)",
        "recommendation": "KEEP_TAG"
      },
      {
        "tag": "Lãng mạn",
        "tagType": "CouplePersonalityType",
        "status": "POOR",
        "severity": "HIGH",
        "totalReviews": 3,
        "matchedCount": 1,
        "unmatchedCount": 2,
        "matchRate": 33.3,
        "message": "⚠️ Tag 'Lãng mạn' KHÔNG phù hợp với venue (chỉ 33.3% khách hàng hài lòng). Nên xóa tag này!",
        "recommendation": "REMOVE_TAG"
      }
    ],
    "summary": {
      "goodTags": ["Yên tĩnh"],
      "warningTags": [],
      "poorTags": ["Lãng mạn"],
      "actionRequired": true,
      "overallMessage": "Venue có 1 tag không phù hợp cần xóa"
    }
  }
}
```

### 2. Admin - Xem config hiện tại

```http
GET /api/admin/venue-tag-config
Authorization: Bearer {token}
Role: ADMIN
```

**Response:**
```json
{
  "success": true,
  "data": {
    "goodThreshold": 70,
    "warningThreshold": 50,
    "minReviews": 3,
    "description": {
      "goodThreshold": ">= 70% = Tag phù hợp (GOOD)",
      "warningThreshold": "50% - 69.9% = Cần xem xét (WARNING)",
      "poor": "< 50% = Không phù hợp (POOR)",
      "minReviews": "Cần ít nhất 3 reviews để đánh giá"
    }
  }
}
```

### 3. Admin - Cập nhật config

```http
PUT /api/admin/venue-tag-config
Authorization: Bearer {token}
Role: ADMIN

Body:
{
  "goodThreshold": 75,
  "warningThreshold": 55,
  "minReviews": 5
}
```

### 4. Admin - Xem phân tích của bất kỳ venue nào

```http
GET /api/admin/venue-tag-analysis/{venueId}
Authorization: Bearer {token}
Role: ADMIN
```

## Cài đặt

### 1. Chạy SQL script để insert default configs

```bash
psql -U postgres -d your_database -f Database/Scripts/insert_venue_tag_analysis_configs.sql
```

### 2. Register services trong Program.cs

```csharp
// Add service
builder.Services.AddScoped<IVenueTagAnalysisService, VenueTagAnalysisService>();
```

### 3. Test API

```bash
# Venue owner xem phân tích
curl -X GET "http://localhost:5000/api/venue-owner/tag-analysis/123" \
  -H "Authorization: Bearer {venue_owner_token}"

# Admin xem config
curl -X GET "http://localhost:5000/api/admin/venue-tag-config" \
  -H "Authorization: Bearer {admin_token}"

# Admin update config
curl -X PUT "http://localhost:5000/api/admin/venue-tag-config" \
  -H "Authorization: Bearer {admin_token}" \
  -H "Content-Type: application/json" \
  -d '{"goodThreshold": 75, "warningThreshold": 55, "minReviews": 5}'
```

## Lưu ý

1. **CoupleMoodSnapshot format:** "PersonalityType,MoodType" (ví dụ: "Lãng mạn,Vui vẻ")
2. **Case-insensitive matching:** So sánh tag không phân biệt hoa thường
3. **Minimum reviews:** Cần ít nhất 3 reviews (hoặc theo config) có tag đó mới đánh giá
4. **Status values:**
   - `GOOD`: Tag phù hợp, giữ nguyên
   - `WARNING`: Tag có vẻ không hoàn toàn phù hợp, xem xét điều chỉnh
   - `POOR`: Tag không phù hợp, nên xóa
   - `INSUFFICIENT_DATA`: Chưa đủ dữ liệu để đánh giá

## Workflow đề xuất

1. Venue owner tạo venue và chọn tags
2. Members đến venue, review và chọn IsMatched
3. Sau khi có >= 3 reviews, venue owner có thể xem phân tích
4. Nếu có tag POOR → xóa tag đó
5. Nếu có tag WARNING → xem xét điều chỉnh không gian/dịch vụ hoặc xóa tag
6. Tags GOOD → giữ nguyên

## Future Enhancements

1. **Auto-notification:** Tự động gửi email cho venue owner khi có tag POOR
2. **Trend analysis:** Theo dõi match rate theo thời gian
3. **Suggested tags:** Gợi ý tags nên thêm dựa trên unmatched reviews
4. **Batch analysis:** Admin có thể chạy phân tích cho tất cả venues
