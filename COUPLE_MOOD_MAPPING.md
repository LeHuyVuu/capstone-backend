# Couple Mood Type Mapping - Business Rules

## Tổng quan

Thay vì đơn giản ghép 2 mood thành 1 mood, hệ thống áp dụng 12 quy tắc phức tạp để map 2 cảm xúc riêng lẻ thành 1 "couple mood type" phù hợp hơn.

### 8 Individual Moods (Từ AWS Rekognition Face Detection)
```
HAPPY      - Hạnh phúc, thoải mái
CALM       - Bình tĩnh, yên tĩnh
SURPRISED  - Ngạc nhiên, hứng thú
CONFUSED   - Bối rối, không rõ ràng
SAD        - Buồn, tủi thân
ANGRY      - Tức giận, nổi dạ
FEAR       - Sợ hãi, lo lắng
DISGUSTED  - Ghét, không chịu được
```

### 12 Couple Mood Types (Output)
```
1. Shared Happiness         - Vui chung, gắn kết
2. Mutual Calm             - Yên tĩnh, nhẹ nhàng
3. Comfort-Seeking         - Cần an ủi, chia sẻ
4. Stress and Tension      - Căng thẳng, cần thoáng
5. Emotional Imbalance     - Cảm xúc lệch pha
6. Exploration Mood        - Hứng thú khám phá
7. Playful but Sensitive   - Vui nhưng dễ tổn thương
8. Reassurance Needed      - Cần được trấn an
9. Low-Intimacy Boundary   - Giảm thân mật
10. Resolution Mode        - Cần hòa giải
11. High-Energy Divergence - Năng lượng không đồng đều
12. Neutral / Mixed Mood   - Trung tính, hỗn hợp
```

---

## 12 Business Rules Chi Tiết

### Rule 1: Shared Happiness (Vui chung)
**Điều kiện**: Ít nhất 1 người HAPPY + người kia không tiêu cực mạnh

**Khi nào áp dụng**:
- HAPPY + HAPPY
- HAPPY + CALM
- HAPPY + SURPRISED
- HAPPY + CONFUSED

**Nhu cầu venue**:
- Vui, gắn kết
- Năng lượng tích cực
- Địa điểm lively, fun activities

---

### Rule 2: Mutual Calm (Yên tĩnh)
**Điều kiện**: Cả hai CALM hoặc một CALM + CONFUSED

**Khi nào áp dụng**:
- CALM + CALM
- CALM + CONFUSED

**Nhu cầu venue**:
- Không gian nhẹ nhàng
- Ít kích thích
- Có thể là spa, cafe yên tĩnh

---

### Rule 3: Comfort-Seeking (Cần an ủi)
**Điều kiện**: 1 người SAD + người kia không ANGRY/DISGUSTED

**Khi nào áp dụng**:
- SAD + HAPPY
- SAD + CALM
- SAD + SURPRISED
- SAD + CONFUSED
- SAD + FEAR

**Nhu cầu venue**:
- Chia sẻ, ấm áp
- Không gian riêng tư
- Nhẹ nhàng, thoải mái

---

### Rule 4: Stress and Tension (Căng thẳng)
**Điều kiện**: Một hoặc cả hai người có ANGRY, FEAR, DISGUSTED

**Khi nào áp dụng**:
- ANGRY + bất kỳ
- FEAR + bất kỳ (ngoại trừ được match với Reassurance)
- DISGUSTED + bất kỳ (ngoại trừ được match với other rules)

**Nhu cầu venue**:
- An toàn
- Không gian thoáng rộng
- Tránh thân mật mạnh
- Có thể là ngoài trời, công viên

---

### Rule 5: Emotional Imbalance (Lệch pha cảm xúc)
**Điều kiện**: 1 người HAPPY vs người kia SAD/ANGRY/FEAR/DISGUSTED

**Khi nào áp dụng**:
- HAPPY + SAD (ngoại trừ nếu match Playful but Sensitive)
- HAPPY + ANGRY → High-Energy Divergence thay vì Imbalance
- HAPPY + FEAR
- HAPPY + DISGUSTED

**Nhu cầu venue**:
- Nơi trung hòa
- Giúp cân bằng cảm xúc
- Có thể chia thành 2 khu vực

---

### Rule 6: Exploration Mood (Hứng thú khám phá)
**Điều kiện**: 1 người SURPRISED + người còn lại HAPPY/CALM/CONFUSED

**Khi nào áp dụng**:
- SURPRISED + HAPPY
- SURPRISED + CALM
- SURPRISED + CONFUSED

**Nhu cầu venue**:
- Khám phá nhẹ
- Địa điểm mới
- Trải nghiệm lạ nhưng an toàn
- New cafe, theme restaurant

---

### Rule 7: Playful but Sensitive (Vui nhưng dễ tổn thương)
**Điều kiện**: Là cặp (HAPPY + SAD), (HAPPY + CONFUSED), (HAPPY + FEAR)

**Khi nào áp dụng**:
- HAPPY + SAD (priority cao hơn Emotional Imbalance)
- HAPPY + CONFUSED
- HAPPY + FEAR

**Nhu cầu venue**:
- Vui nhưng không quá mạnh
- Tránh đùa quá gay gắt
- Có chỗ yên tĩnh để retreat
- Activity có thể calm down

---

### Rule 8: Reassurance Needed (Cần được trấn an)
**Điều kiện**: 1 người FEAR/CONFUSED + người kia CALM/SURPRISED

**Khi nào áp dụng**:
- FEAR + CALM
- FEAR + SURPRISED
- CONFUSED + CALM
- CONFUSED + SURPRISED

**Nhu cầu venue**:
- An toàn, ấm áp
- Không đông đúc
- Có person can reassure
- Intimate nhưng relaxing

---

### Rule 9: Low-Intimacy Boundary (Giảm thân mật)
**Điều kiện**: Bất kỳ người nào DISGUSTED

**Khi nào áp dụng**:
- DISGUSTED + bất kỳ (except nếu match Resolution Mode)
- Ưu tiên cao, được check trước

**Nhu cầu venue**:
- Tránh không gian kín
- Tránh tiếp xúc quá gần
- Thoáng, có khoảng cách
- Ngoài trời tốt

---

### Rule 10: Resolution Mode (Cần hòa giải)
**Điều kiện**: ANGRY + SAD, ANGRY + CONFUSED, SAD + DISGUSTED

**Khi nào áp dụng**:
- ANGRY + SAD
- ANGRY + CONFUSED
- SAD + DISGUSTED

**Nhu cầu venue**:
- Không gian trung lập
- Dễ nói chuyện
- Không quá intimate
- Có thể walk and talk venue

---

### Rule 11: High-Energy Divergence (Năng lượng không đồng đều)
**Điều kiện**: HAPPY + ANGRY hoặc SURPRISED + ANGRY

**Khi nào áp dụng**:
- HAPPY + ANGRY (priority cao hơn Emotional Imbalance)
- SURPRISED + ANGRY

**Nhu cầu venue**:
- Nơi rộng, thoáng
- Có thể chia nhóm
- Tránh kích thích thêm
- Calm activities

---

### Rule 12: Neutral / Mixed Mood (Trung tính)
**Điều kiện**: Fallback cho các cặp không match rule nào

**Khi nào áp dụng**:
- CONFUSED + CALM
- CONFUSED + SURPRISED
- Các cặp không match rule khác
- Default fallback

**Nhu cầu venue**:
- Nhẹ nhàng, trung lập
- Không quá nhiều stimulus
- Flexible, có thể adapt
- Cafe, casual dining

---

## Implementation Priority

```
Rule 9 (Low-Intimacy Boundary) - Highest Priority
  ↓
Rule 1 (Shared Happiness)
Rule 2 (Mutual Calm)
Rule 3 (Comfort-Seeking)
Rule 4 (Stress and Tension)
  ↓
Rule 7 (Playful but Sensitive)
Rule 8 (Reassurance Needed)
Rule 10 (Resolution Mode)
Rule 11 (High-Energy Divergence)
  ↓
Rule 6 (Exploration Mood)
Rule 5 (Emotional Imbalance)
  ↓
Rule 12 (Neutral / Mixed Mood) - Fallback (Lowest Priority)
```

---

## Code Structure

File mới: `CoupleMoodMapper.cs`
- Static class
- Method: `MapToCoupleeMood(string mood1, string mood2) -> string`
- 12 private rule methods

File cập nhật: `MoodMappingService.cs`
- Gọi `CoupleMoodMapper.MapToCoupleeMood()`
- Không còn logic switch cũ
- Clean & focused

---

## Examples

### Example 1: Happy + Sad
```
Input: mood1 = "HAPPY", mood2 = "SAD"
Check: Playful but Sensitive rule? → YES (Priority 7)
Output: "Playful but Sensitive"
Venue: Fun activities but gentle, có space to calm down
```

### Example 2: Angry + Any
```
Input: mood1 = "ANGRY", mood2 = "CALM"
Check: Low-Intimacy? → NO
Check: Stress and Tension? → YES (ANGRY present)
Output: "Stress and Tension"
Venue: Open space, outdoor
```

### Example 3: Afraid + Calm
```
Input: mood1 = "FEAR", mood2 = "CALM"
Check: Low-Intimacy? → NO
Check: Reassurance Needed? → YES (FEAR + CALM)
Output: "Reassurance Needed"
Venue: Safe, intimate, warm
```

---

## Migration Notes

### Old System (Cũ)
- Simple 4-5 mood types
- Limited business logic
- Doesn't account for emotional dynamics

### New System (Mới)
- 12 specific couple mood types
- Covers 64 combinations down to meaningful categories
- Captures emotional relationship dynamics
- Better venue recommendations

### Database Update Needed
Cần add/update CoupleMoodType table với 12 giá trị mới:
1. Shared Happiness
2. Mutual Calm
3. Comfort-Seeking
4. Stress and Tension
5. Emotional Imbalance
6. Exploration Mood
7. Playful but Sensitive
8. Reassurance Needed
9. Low-Intimacy Boundary
10. Resolution Mode
11. High-Energy Divergence
12. Neutral / Mixed Mood
