# 🗺️ HỆ THỐNG THEO DÕI VỊ TRÍ REAL-TIME - GIẢI PHÁP ĐơN GIẢN

## 📋 KIẾN TRÚC

```
┌─────────────────┐
│  Flutter App    │
│  - GPS Stream   │
│  - Google Maps  │
└─────┬─────┬─────┘
      │     │
      │     └──────────────────┐
      │                        │
      │ REST API               │ Firebase Realtime DB
      │ (Watchlist)            │ (Real-time Locations)
      ▼                        ▼
┌─────────────┐         ┌──────────────────┐
│ .NET Backend│         │ Firebase Realtime│
│ PostgreSQL  │         │ /locations/{uid} │
│ watchlist   │         │ /watch/{uid}     │
└─────────────┘         └──────────────────┘
```

**Backend chỉ quản lý watchlist relationships (PostgreSQL)**  
**Firebase Realtime Database do Flutter tự xử lý trực tiếp**

---

## 🎯 BACKEND ĐÃ CODE (4 FILES)

### 1. **LocationFollowerDto.cs** - DTO đơn giản
```csharp
- LocationFollowerDto (thông tin user)
- WatchlistRequest (request add/remove)
```

### 2. **ILocationFollowerService.cs + LocationFollowerService.cs** - Service đơn giản
```csharp
✅ AddToWatchlistAsync() - Thêm vào watchlist
✅ RemoveFromWatchlistAsync() - Xóa khỏi watchlist
✅ GetMyWatchlistAsync() - Lấy danh sách đang theo dõi
✅ GetMyFollowersAsync() - Lấy người theo dõi mình
```

### 3. **LocationTrackingController.cs** - 4 API endpoints
```
POST /api/LocationTracking/watchlist/add
POST /api/LocationTracking/watchlist/remove
GET  /api/LocationTracking/watchlist
GET  /api/LocationTracking/followers
```

### 4. **ServiceExtensions.cs** - Đã register service

---

## 🚀 CÁCH HOẠT ĐỘNG

### **Backend (.NET):**
- Chỉ quản lý **watchlist relationships** trong PostgreSQL
- Sử dụng table `location_follower` có sẵn
- Không cần Firebase Admin SDK
- Đơn giản, nhẹ, dễ maintain

### **Flutter App:**
1. **Login** → Lấy JWT token từ backend
2. **Get Watchlist** → Call `GET /api/LocationTracking/watchlist`
3. **Authenticate Firebase** → Dùng Firebase Auth (email/password hoặc anonymous)
4. **Write Location** → `DatabaseReference.child('locations/$myUid').set({lat, lng})`
5. **Listen Watchlist** → For each user in watchlist, listen to `locations/$targetUid`
6. **Update Map** → Show markers real-time

---

## 📱 FLUTTER CODE MẪU

### **Setup Firebase (pubspec.yaml)**
```yaml
dependencies:
  firebase_core: ^2.24.0
  firebase_auth: ^4.15.0
  firebase_database: ^10.4.0
  geolocator: ^10.1.0
  google_maps_flutter: ^2.5.0
```

### **1. Get Watchlist từ Backend**
```dart
Future<List<int>> getWatchlist() async {
  final response = await http.get(
    Uri.parse('$baseUrl/api/LocationTracking/watchlist'),
    headers: {'Authorization': 'Bearer $jwtToken'},
  );
  
  final data = jsonDecode(response.body)['data'] as List;
  return data.map((item) => item['userId'] as int).toList();
}
```

### **2. Authenticate Firebase**
```dart
// Login anonymous hoặc email/password
await FirebaseAuth.instance.signInAnonymously();
// Hoặc:
await FirebaseAuth.instance.signInWithEmailAndPassword(email, password);
```

### **3. Upload Vị Trí Của Mình**
```dart
final db = FirebaseDatabase.instance.ref();
final myUid = FirebaseAuth.instance.currentUser!.uid;

Geolocator.getPositionStream(
  locationSettings: LocationSettings(
    accuracy: LocationAccuracy.high,
    distanceFilter: 20, // 20 meters
  ),
).listen((Position position) {
  db.child('locations/$myUid').set({
    'lat': position.latitude,
    'lng': position.longitude,
    'updatedAt': DateTime.now().millisecondsSinceEpoch,
  });
});
```

### **4. Listen Vị Trí Người Khác**
```dart
// Lấy watchlist từ backend
final watchlist = await getWatchlist();

// Listen location của từng người trong watchlist
for (int targetUserId in watchlist) {
  final targetUid = targetUserId.toString(); // Hoặc map user_id → firebase_uid
  
  db.child('locations/$targetUid').onValue.listen((event) {
    if (event.snapshot.value != null) {
      final data = event.snapshot.value as Map;
      final lat = data['lat'];
      final lng = data['lng'];
      
      // Cập nhật marker trên Google Map
      setState(() {
        markers[targetUserId] = Marker(
          markerId: MarkerId('user_$targetUserId'),
          position: LatLng(lat, lng),
        );
      });
    }
  });
}
```

### **5. Khi Rời Màn Map - Dọn Dẹp**
```dart
@override
void dispose() {
  // Stop GPS stream
  positionStream?.cancel();
  
  // Remove vị trí khỏi Firebase
  final myUid = FirebaseAuth.instance.currentUser!.uid;
  FirebaseDatabase.instance.ref().child('locations/$myUid').remove();
  
  super.dispose();
}
```

---

## 🔐 FIREBASE SECURITY RULES

Tạo rules đơn giản trong Firebase Console:

```json
{
  "rules": {
    "locations": {
      "$uid": {
        ".read": "auth != null",
        ".write": "auth != null && auth.uid == $uid"
      }
    }
  }
}
```

**Giải thích:**
- Ai cũng đọc được location (vì backend đã quản lý watchlist)
- Chỉ user đó mới ghi được location của mình

---

## ✅ TESTING

### **1. Test Backend API**
```bash
# Login
curl -X POST http://localhost:5224/api/Auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"password"}'

# Add to watchlist
curl -X POST http://localhost:5224/api/LocationTracking/watchlist/add \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"targetUserId":456}'

# Get watchlist
curl -X GET http://localhost:5224/api/LocationTracking/watchlist \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### **2. Test Firebase từ Flutter**
```dart
// Test write
await FirebaseDatabase.instance.ref().child('locations/test').set({
  'lat': 10.762622,
  'lng': 106.660172,
});

// Test read
final snapshot = await FirebaseDatabase.instance.ref().child('locations/test').get();
print(snapshot.value);
```

---

## 🎯 LOGIC HOẠT ĐỘNG

### **Màn Map - onInit:**
1. ✅ Call backend lấy watchlist
2. ✅ Bật GPS stream → ghi location vào Firebase
3. ✅ For each user trong watchlist → listen location từ Firebase
4. ✅ Update markers trên Google Map real-time

### **Thêm User Vào Watchlist:**
1. ✅ Call `POST /api/LocationTracking/watchlist/add`
2. ✅ Backend lưu vào PostgreSQL
3. ✅ Flutter tự động listen location của user mới

### **Rời Màn Map - onDispose:**
1. ✅ Stop GPS stream
2. ✅ Remove location khỏi Firebase
3. ✅ Cancel all listeners

---

## 📦 TÓM TẮT

**✅ Đơn giản:** Chỉ 4 files backend  
**✅ Dễ hiểu:** Backend quản lý watchlist, Firebase quản lý real-time  
**✅ Nhẹ:** Không cần Firebase Admin SDK  
**✅ Hiệu quả:** Real-time sync tốt qua Firebase  
**✅ Bảo mật:** Firebase rules + JWT backend  

**Backend chỉ làm:** CRUD watchlist relationships  
**Flutter tự làm:** GPS + Firebase Realtime Database + Google Maps

---

## 🔧 CHẠY BACKEND

```bash
dotnet restore
dotnet run
```

Truy cập Swagger: http://localhost:5224/scalar

---

**🎉 DONE! Backend đơn giản, Flutter tự xử lý Firebase!**
