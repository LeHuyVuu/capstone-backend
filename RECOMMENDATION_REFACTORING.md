# Recommendation Feature Refactoring Summary

## Tổng quan refactoring

Feature Recommendation API đã được refactor thành cấu trúc gọn gàng, dễ đọc và dễ bảo trì hơn. Đoạn code dài từ 704 dòng được chia thành các helper files chuyên biệt.

## Cấu trúc mới

### 📦 Các files chính:

#### 1. **OpenAIRecommendationService.cs** (Core Orchestrator)
- **Role**: Điều phối luồng recommendation, không chứa logic chi tiết
- **Main method**: `GetRecommendationsAsync()` - xếp chỉnh 8 phases
- **Dòng code**: ~380 (giảm từ 704)
- **Trách nhiệm**:
  - Quản lý dependencies (IUnitOfWork, services, ChatClient)
  - Điều phối các bước recommendation workflow
  - Xử lý error handling & fallback

#### 2. **QueryParser.cs** (Static Helper)
- **Role**: Phân tích natural language queries bằng AI
- **Public Methods**:
  - `ParseQueryWithAIAsync()` - Parse query thành structured context
- **Output**: `ParsedQueryContext` (Intent, DetectedMood, Tags, Region)
- **Lợi ích**:
  - Tách biệt logic parse query
  - Dễ test và reuse
  - Dễ update prompts parsing mà không affect code chính

#### 3. **PromptBuilder.cs** (Static Helper)
- **Role**: Xây dựng prompts cho OpenAI API
- **Public Methods**:
  - `BuildSystemPrompt()` - System prompt cho AI
  - `BuildUserPrompt()` - User prompt từ venue + context
- **Lợi ích**:
  - Quản lý all prompts ở một chỗ
  - Dễ update/optimize AI prompts
  - Rõ ràng logic prompt construction

#### 4. **ResponseFormatter.cs** (Static Helper)
- **Role**: Parse & format responses từ OpenAI
- **Public Methods**:
  - `ParseAIResponse()` - Parse AI response thành Dictionary
  - `GenerateDefaultExplanation()` - Fallback explanation
- **Lợi ích**:
  - Tách biệt logic parsing response
  - Dễ maintain explanation logic
  - Reusable cho request khác

#### 5. **VenueContextBuilder.cs** (Static Helper)
- **Role**: Xây dựng venue context string cho AI
- **Public Methods**:
  - `BuildVenueContext()` - Format venues + scores thành string
- **Lợi ích**:
  - Tách biệt logic venue context formatting
  - Dễ modify output format cho AI
  - Readable context construction

#### 6. **RecommendationFormatter.cs** (Static Helper)
- **Role**: Format final recommendation responses
- **Public Methods**:
  - `FormatRecommendedVenues()` - Format ranked venues thành response
  - `FormatFallbackVenues()` - Format fallback venues
- **Lợi ích**:
  - Tách biệt logic response formatting
  - Reusable mapping logic
  - Easy to modify DTO mapping

---

## So sánh Before & After

### Before (Single File)
```
OpenAIRecommendationService.cs
├── ParseQueryWithAIAsync() - Parse natural language
├── GetCoupleMoodTypeAsync() - Get mood
├── BuildVenueContext() - Build context
├── BuildSystemPrompt() - System prompt
├── BuildUserPrompt() - User prompt
├── ParseAIResponse() - Parse response
├── GenerateDefaultExplanation() - Default text
├── GetAIExplanationsWithTimeoutAsync() - Call OpenAI
├── RetrieveCandidateVenuesSmartAsync() - Smart retrieval
└── GetFallbackRecommendationsAsync() - Fallback
```

### After (Modular Structure)
```
Business/
├── Services/
│   └── OpenAIRecommendationService.cs (380 lines, Orchestrator)
│
└── Recommendation/
    ├── QueryParser.cs (Static - Parse queries)
    ├── PromptBuilder.cs (Static - Build prompts)
    ├── ResponseFormatter.cs (Static - Format responses)
    ├── VenueContextBuilder.cs (Static - Build venue context)
    └── RecommendationFormatter.cs (Static - Format recommendations)
```

---

## Logic Flow Diagram

```
GetRecommendationsAsync()
│
├─► Phase 1-2: Parallel
│   ├─► QueryParser.ParseQueryWithAIAsync() ──┐
│   └─► GetCoupleMoodTypeAsync()               │
│                                              ├─► Merge contexts
│                                              │
├─► Phase 3: Map MBTI → Personality Tags ──┐  │
│                                           │  │
├─► Phase 4: RetrieveCandidateVenues ──┐   │  │
│                                       │   │  │
├─► Phase 5: Score & Rank Venues ──┐   │   │  │
│                                   │   │   │  │
├─► Phase 6: VenueContextBuilder.BuildVenueContext() ─┐
│                                                     │
├─► Phase 7: GetAIExplanationsWithTimeoutAsync()      │
│   ├─► PromptBuilder.BuildSystemPrompt()            │
│   ├─► PromptBuilder.BuildUserPrompt()   ─┐         │
│   └─► ResponseFormatter.ParseAIResponse() │         │
│                                           ├─► Call OpenAI
├─► Phase 8: RecommendationFormatter.FormatRecommendedVenues()
│
└─► Return RecommendationResponse
```

---

## Các lợi ích của refactoring

✅ **Modular**: Mỗi file có trách nhiệm riêng  
✅ **Testable**: Static methods dễ unit test hơn  
✅ **Maintainable**: Logic tách rời dễ bảo trì  
✅ **Reusable**: Helper classes có thể dùng ở các feature khác  
✅ **Readable**: Service file từ 704 → 380 dòng, dễ hiểu  
✅ **Extensible**: Dễ thêm features mới mà không affect codebase  
✅ **Performance**: Không thay đổi, vẫn giữ parallel execution & optimization  

---

## Giữ nguyên các điểm chính

✔️ **Logic không đổi**: Tất cả logic gốc vẫn như cũ  
✔️ **Performance**: Vẫn dùng parallel execution, async/await  
✔️ **Error handling**: Vẫn có fallback mechanisms  
✔️ **Interface**: IRecommendationService không thay đổi  
✔️ **Database queries**: Vẫn sử dụng UnitOfWork pattern  
✔️ **Compatibility**: Không ảnh hưởng đến các files khác  

---

## Hướng dùng

Các static helper classes được thiết kế để dùng như:

```csharp
// QueryParser
var parsedContext = await QueryParser.ParseQueryWithAIAsync(request, chatClient, logger);

// PromptBuilder
var systemPrompt = PromptBuilder.BuildSystemPrompt();
var userPrompt = PromptBuilder.BuildUserPrompt(...);

// ResponseFormatter
var explanations = ResponseFormatter.ParseAIResponse(aiResponse);
var defaultExpl = ResponseFormatter.GenerateDefaultExplanation(...);

// VenueContextBuilder
var context = VenueContextBuilder.BuildVenueContext(...);

// RecommendationFormatter
var recommendations = RecommendationFormatter.FormatRecommendedVenues(...);
```

---

## Testing notes

- Các static methods dễ mock trong unit tests
- Service orchestrator tập trung vào flow logic, dễ test integration
- Từng helper class có thể test độc lập
- Refactoring không thay đổi external behavior
