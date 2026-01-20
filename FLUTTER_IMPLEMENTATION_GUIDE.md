# 📱 FLUTTER MOBILE - HƯỚNG DẪN IMPLEMENTATION THEO DÕI VỊ TRÍ REAL-TIME

## 🎯 MỤC TIÊU
Tạo màn hình Map hiển thị vị trí real-time của bạn và những người trong watchlist, tương tự Jagat.

---

## ⚙️ BƯỚC 1: SETUP DEPENDENCIES

### **pubspec.yaml**
```yaml
dependencies:
  flutter:
    sdk: flutter
  
  # HTTP & Authentication
  http: ^1.1.0
  shared_preferences: ^2.2.2
  
  # Firebase
  firebase_core: ^2.24.2
  firebase_auth: ^4.16.0
  firebase_database: ^10.4.0
  
  # Location & Maps
  geolocator: ^10.1.0
  google_maps_flutter: ^2.5.3
  permission_handler: ^11.1.0
```

### **Chạy:**
```bash
flutter pub get
```

---

## 🔥 BƯỚC 2: SETUP FIREBASE

### **2.1. Tạo Firebase Project**
1. Vào https://console.firebase.google.com/
2. Tạo project mới hoặc dùng project có sẵn
3. Thêm Android/iOS app

### **2.2. Download config files**

**Android:** Tải `google-services.json` → đặt vào `android/app/`

**iOS:** Tải `GoogleService-Info.plist` → đặt vào `ios/Runner/`

### **2.3. Enable Firebase Realtime Database**
1. Trong Firebase Console → Realtime Database
2. Chọn location: `asia-southeast1` (gần Việt Nam)
3. Start in **test mode** (sẽ setup rules sau)

### **2.4. Firebase Security Rules**
Vào Firebase Console → Realtime Database → Rules, paste code sau:

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
- Ai đã login đều đọc được location (backend đã quản lý watchlist)
- Chỉ user đó mới ghi được location của chính mình

### **2.5. Enable Authentication**
1. Firebase Console → Authentication
2. Enable **Email/Password** hoặc **Anonymous** 
3. (Recommended) Enable cả hai

---

## 📱 BƯỚC 3: ANDROID SETUP

### **android/app/build.gradle**
```gradle
android {
    defaultConfig {
        minSdkVersion 21  // Quan trọng! Phải >= 21
        targetSdkVersion 34
    }
}

dependencies {
    implementation platform('com.google.firebase:firebase-bom:32.7.0')
}
```

### **android/app/src/main/AndroidManifest.xml**
```xml
<manifest>
    <!-- Thêm permissions -->
    <uses-permission android:name="android.permission.INTERNET"/>
    <uses-permission android:name="android.permission.ACCESS_FINE_LOCATION"/>
    <uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION"/>
    <uses-permission android:name="android.permission.ACCESS_BACKGROUND_LOCATION"/>
    
    <application>
        <!-- Google Maps API Key -->
        <meta-data
            android:name="com.google.android.geo.API_KEY"
            android:value="YOUR_GOOGLE_MAPS_API_KEY_HERE"/>
    </application>
</manifest>
```

### **Lấy Google Maps API Key:**
1. Vào https://console.cloud.google.com/
2. Enable **Maps SDK for Android** và **Maps SDK for iOS**
3. Tạo API key → Paste vào AndroidManifest.xml

---

## 🍎 BƯỚC 4: iOS SETUP

### **ios/Runner/Info.plist**
```xml
<dict>
    <!-- Location permissions -->
    <key>NSLocationWhenInUseUsageDescription</key>
    <string>Ứng dụng cần vị trí để hiển thị bản đồ và chia sẻ với bạn bè</string>
    
    <key>NSLocationAlwaysAndWhenInUseUsageDescription</key>
    <string>Ứng dụng cần vị trí để theo dõi và chia sẻ vị trí real-time</string>
    
    <key>NSLocationAlwaysUsageDescription</key>
    <string>Ứng dụng cần vị trí ngay cả khi ở background</string>
</dict>
```

---

## 💻 BƯỚC 5: CODE FLUTTER

### **5.1. main.dart - Initialize Firebase**
```dart
import 'package:flutter/material.dart';
import 'package:firebase_core/firebase_core.dart';
import 'firebase_options.dart'; // Auto-generated
import 'screens/map_screen.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await Firebase.initializeApp(
    options: DefaultFirebaseOptions.currentPlatform,
  );
  runApp(MyApp());
}

class MyApp extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Location Tracking',
      home: MapScreen(),
    );
  }
}
```

**Chạy FlutterFire CLI để generate firebase_options.dart:**
```bash
# Install FlutterFire CLI
dart pub global activate flutterfire_cli

# Generate config
flutterfire configure
```

---

## 🔌 BƯỚC 5A: CÁCH HOẠT ĐỘNG VỚI BACKEND

### **Luồng hoạt động:**
```
1. Login → Nhận JWT token → Lưu vào SharedPreferences
2. Mỗi API call → Lấy token → Gửi trong header Authorization
3. Backend verify token → Trả về data
```

### **Base URL theo môi trường:**
```dart

// Production
https://localhost:5224/api
```

### **Cách lấy IP máy Windows:**
```powershell
# Chạy trong PowerShell
ipconfig

# Tìm dòng "IPv4 Address" trong phần WiFi adapter
# Ví dụ: 192.168.1.100
```

### **Response format từ backend:**
```json
{
  "message": "Success",
  "code": 200,
  "data": { /* your data */ },
  "traceId": "xxx"
}
```

---

### **5.2. services/api_service.dart - Backend API**
```dart
import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

class ApiService {
  // ⚠️ QUAN TRỌNG: Thay đổi theo môi trường của bạn
  static const String baseUrl = 'http://10.0.2.2:5224/api'; // Android Emulator
  // static const String baseUrl = 'http://localhost:5224/api'; // iOS Simulator
  // static const String baseUrl = 'http://192.168.1.100:5224/api'; // Real Device
  // static const String baseUrl = 'https://your-backend.com/api'; // Production

  // Lấy JWT token từ SharedPreferences
  Future<String?> _getToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString('jwt_token');
  }

  // Login và lưu token
  Future<bool> login(String email, String password) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/Auth/login'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({'email': email, 'password': password}),
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        final token = data['data']['accessToken'];
        
        final prefs = await SharedPreferences.getInstance();
        await prefs.setString('jwt_token', token);
        
        return true;
      }
      return false;
    } catch (e) {
      print('Login error: $e');
      return false;
    }
  }

  // Lấy watchlist từ backend
  Future<List<int>> getWatchlist() async {
    try {
      final token = await _getToken();
      if (token == null) return [];

      final response = await http.get(
        Uri.parse('$baseUrl/LocationTracking/watchlist'),
        headers: {'Authorization': 'Bearer $token'},
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        final List items = data['data'] ?? [];
        return items.map((item) => item['userId'] as int).toList();
      }
      return [];
    } catch (e) {
      print('Get watchlist error: $e');
      return [];
    }
  }

  // Thêm vào watchlist
  Future<bool> addToWatchlist(int targetUserId) async {
    try {
      final token = await _getToken();
      if (token == null) return false;

      final response = await http.post(
        Uri.parse('$baseUrl/LocationTracking/watchlist/add'),
        headers: {
          'Authorization': 'Bearer $token',
          'Content-Type': 'application/json',
        },
        body: jsonEncode({'targetUserId': targetUserId}),
      );

      return response.statusCode == 200;
    } catch (e) {
      print('Add to watchlist error: $e');
      return false;
    }
  }

  // Xóa khỏi watchlist
  Future<bool> removeFromWatchlist(int targetUserId) async {
    try {
      final token = await _getToken();
      if (token == null) return false;

      final response = await http.post(
        Uri.parse('$baseUrl/LocationTracking/watchlist/remove'),
        headers: {
          'Authorization': 'Bearer $token',
          'Content-Type': 'application/json',
        },
        body: jsonEncode({'targetUserId': targetUserId}),
      );

      return response.statusCode == 200;
    } catch (e) {
      print('Remove from watchlist error: $e');
      return false;
    }
  }

  // Lấy thông tin user hiện tại (để test token có hoạt động không)
  Future<Map<String, dynamic>?> getCurrentUser() async {
    try {
      final token = await _getToken();
      if (token == null) return null;

      final response = await http.get(
        Uri.parse('$baseUrl/Users/me'), // Endpoint này phải có ở backend
        headers: {'Authorization': 'Bearer $token'},
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        return data['data'];
      }
      return null;
    } catch (e) {
      print('Get current user error: $e');
      return null;
    }
  }
}
```

---

## 📞 CÁCH GỌI API TỪ FLUTTER

### **1. Login (Bước đầu tiên - BẮT BUỘC)**
```dart
// Trong login screen
final apiService = ApiService();

// Login và lưu token
bool success = await apiService.login('user@example.com', 'password123');

if (success) {
  // Token đã được lưu tự động, giờ có thể gọi các API khác
  Navigator.pushReplacement(context, MaterialPageRoute(builder: (_) => MapScreen()));
} else {
  // Hiển thị lỗi
  ScaffoldMessenger.of(context).showSnackBar(
    SnackBar(content: Text('Login thất bại')),
  );
}
```

### **2. Lấy Watchlist**
```dart
final apiService = ApiService();

// Lấy danh sách user ID đang theo dõi
List<int> watchlist = await apiService.getWatchlist();

print('Watchlist: $watchlist'); // [123, 456, 789]

// Dùng để listen location từ Firebase
for (int userId in watchlist) {
  locationService.listenToUser(userId);
}
```

### **3. Thêm User vào Watchlist**
```dart
final apiService = ApiService();

// Thêm user 456
bool success = await apiService.addToWatchlist(456);

if (success) {
  print('✅ Đã thêm user 456 vào watchlist');
  
  // Bắt đầu listen location của user này
  locationService.listenToUser(456);
} else {
  print('❌ Thêm thất bại');
}
```

### **4. Xóa User khỏi Watchlist**
```dart
final apiService = ApiService();

// Xóa user 456
bool success = await apiService.removeFromWatchlist(456);

if (success) {
  print('✅ Đã xóa user 456 khỏi watchlist');
  
  // Ngừng listen location
  locationService.stopListeningToUser(456);
  
  // Xóa marker trên map
  setState(() {
    markers.remove(456);
  });
}
```

### **5. Luồng Hoàn Chỉnh trong MapScreen**
```dart
class _MapScreenState extends State<MapScreen> {
  final ApiService _apiService = ApiService();
  final LocationService _locationService = LocationService();
  
  @override
  void initState() {
    super.initState();
    _initializeApp();
  }
  
  Future<void> _initializeApp() async {
    // 1. Initialize Firebase & Location
    await _locationService.initialize();
    
    // 2. Start upload vị trí của mình
    _locationService.startUploadingLocation();
    
    // 3. Lấy watchlist từ backend
    List<int> watchlist = await _apiService.getWatchlist();
    
    // 4. Listen location của từng user trong watchlist
    for (int userId in watchlist) {
      _locationService.listenToUser(userId);
    }
    
    // 5. Setup callback khi nhận được location update
    _locationService.onLocationUpdate = (userId, latLng) {
      setState(() {
        markers[userId] = Marker(
          markerId: MarkerId('user_$userId'),
          position: latLng,
        );
      });
    };
  }
  
  // Thêm user mới vào watchlist
  Future<void> _addUserToWatchlist(int userId) async {
    bool success = await _apiService.addToWatchlist(userId);
    
    if (success) {
      // Listen location ngay lập tức
      _locationService.listenToUser(userId);
      
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Đã thêm user $userId vào watchlist')),
      );
    }
  }
}
```

---

### **5.3. services/location_service.dart - GPS & Firebase**
```dart
import 'dart:async';
import 'package:geolocator/geolocator.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'package:firebase_database/firebase_database.dart';
import 'package:google_maps_flutter/google_maps_flutter.dart';

class LocationService {
  final DatabaseReference _db = FirebaseDatabase.instance.ref();
  StreamSubscription<Position>? _positionStream;
  Map<int, StreamSubscription<DatabaseEvent>> _locationListeners = {};
  
  String? _myUid;
  Function(int userId, LatLng position)? onLocationUpdate;
  Function(int userId)? onLocationRemoved;

  // Khởi tạo và xin quyền location
  Future<bool> initialize() async {
    try {
      // Kiểm tra GPS service
      bool serviceEnabled = await Geolocator.isLocationServiceEnabled();
      if (!serviceEnabled) {
        throw Exception('GPS chưa được bật. Vui lòng bật GPS.');
      }

      // Xin quyền location
      LocationPermission permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
        if (permission == LocationPermission.denied) {
          throw Exception('Quyền location bị từ chối');
        }
      }

      if (permission == LocationPermission.deniedForever) {
        throw Exception('Quyền location bị từ chối vĩnh viễn');
      }

      // Authenticate Firebase (Anonymous)
      final userCredential = await FirebaseAuth.instance.signInAnonymously();
      _myUid = userCredential.user?.uid;

      return true;
    } catch (e) {
      print('Initialize error: $e');
      return false;
    }
  }

  // Bắt đầu upload vị trí của mình
  void startUploadingLocation() {
    if (_myUid == null) return;

    _positionStream = Geolocator.getPositionStream(
      locationSettings: const LocationSettings(
        accuracy: LocationAccuracy.high,
        distanceFilter: 20, // Update mỗi 20 meters
      ),
    ).listen((Position position) {
      _db.child('locations/$_myUid').set({
        'lat': position.latitude,
        'lng': position.longitude,
        'updatedAt': DateTime.now().millisecondsSinceEpoch,
      });
      
      print('📍 Uploaded location: ${position.latitude}, ${position.longitude}');
    });
  }

  // Nghe location của 1 user
  void listenToUser(int userId) {
    if (_locationListeners.containsKey(userId)) return;

    final String firebaseUid = userId.toString(); // Hoặc map user_id → firebase_uid
    
    final listener = _db.child('locations/$firebaseUid').onValue.listen((event) {
      if (event.snapshot.value != null) {
        final data = event.snapshot.value as Map;
        final lat = data['lat'] as double;
        final lng = data['lng'] as double;
        
        onLocationUpdate?.call(userId, LatLng(lat, lng));
      } else {
        onLocationRemoved?.call(userId);
      }
    });

    _locationListeners[userId] = listener;
  }

  // Ngừng nghe location của 1 user
  void stopListeningToUser(int userId) {
    _locationListeners[userId]?.cancel();
    _locationListeners.remove(userId);
  }

  // Cleanup khi rời màn hình
  Future<void> dispose() async {
    // Stop GPS stream
    await _positionStream?.cancel();

    // Stop all location listeners
    for (var listener in _locationListeners.values) {
      await listener.cancel();
    }
    _locationListeners.clear();

    // Remove my location from Firebase
    if (_myUid != null) {
      await _db.child('locations/$_myUid').remove();
    }
  }
}
```

---

### **5.4. screens/map_screen.dart - UI Screen**
```dart
import 'package:flutter/material.dart';
import 'package:google_maps_flutter/google_maps_flutter.dart';
import 'package:geolocator/geolocator.dart';
import '../services/api_service.dart';
import '../services/location_service.dart';

class MapScreen extends StatefulWidget {
  @override
  _MapScreenState createState() => _MapScreenState();
}

class _MapScreenState extends State<MapScreen> {
  GoogleMapController? _mapController;
  final ApiService _apiService = ApiService();
  final LocationService _locationService = LocationService();
  
  LatLng _currentPosition = LatLng(10.762622, 106.660172); // Default: Saigon
  Map<int, Marker> _markers = {};
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _initialize();
  }

  Future<void> _initialize() async {
    try {
      // 1. Initialize location service
      bool initialized = await _locationService.initialize();
      if (!initialized) {
        _showError('Không thể khởi tạo location service');
        return;
      }

      // 2. Get current position
      Position position = await Geolocator.getCurrentPosition();
      setState(() {
        _currentPosition = LatLng(position.latitude, position.longitude);
      });

      // 3. Start uploading my location
      _locationService.startUploadingLocation();

      // 4. Setup location update callback
      _locationService.onLocationUpdate = (userId, latLng) {
        setState(() {
          _markers[userId] = Marker(
            markerId: MarkerId('user_$userId'),
            position: latLng,
            icon: BitmapDescriptor.defaultMarkerWithHue(BitmapDescriptor.hueBlue),
            infoWindow: InfoWindow(title: 'User $userId'),
          );
        });
      };

      _locationService.onLocationRemoved = (userId) {
        setState(() {
          _markers.remove(userId);
        });
      };

      // 5. Load watchlist and listen to locations
      await _loadWatchlist();

      setState(() {
        _isLoading = false;
      });
    } catch (e) {
      _showError('Lỗi khởi tạo: $e');
    }
  }

  Future<void> _loadWatchlist() async {
    try {
      final watchlist = await _apiService.getWatchlist();
      
      for (int userId in watchlist) {
        _locationService.listenToUser(userId);
      }
      
      print('✅ Loaded watchlist: $watchlist');
    } catch (e) {
      print('Load watchlist error: $e');
    }
  }

  void _showError(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(message), backgroundColor: Colors.red),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Location Tracking'),
        actions: [
          IconButton(
            icon: Icon(Icons.refresh),
            onPressed: _loadWatchlist,
          ),
        ],
      ),
      body: _isLoading
          ? Center(child: CircularProgressIndicator())
          : GoogleMap(
              initialCameraPosition: CameraPosition(
                target: _currentPosition,
                zoom: 14,
              ),
              onMapCreated: (controller) {
                _mapController = controller;
              },
              markers: Set<Marker>.of(_markers.values),
              myLocationEnabled: true,
              myLocationButtonEnabled: true,
            ),
      floatingActionButton: FloatingActionButton(
        onPressed: () async {
          // Demo: Add user 456 vào watchlist
          bool success = await _apiService.addToWatchlist(456);
          if (success) {
            _locationService.listenToUser(456);
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(content: Text('Đã thêm user 456 vào watchlist')),
            );
          }
        },
        child: Icon(Icons.add),
      ),
    );
  }

  @override
  void dispose() {
    _locationService.dispose();
    super.dispose();
  }
}
```

---

## ✅ BƯỚC 6: TESTING

### **6.1. Test GPS Permission**
```dart
// Chạy app → cho phép location permission
```

### **6.2. Test Backend Connection**
```dart
// Đảm bảo backend đang chạy
// Android Emulator: http://10.0.2.2:5224
// iOS Simulator: http://localhost:5224
```

### **6.3. Test Firebase**
```dart
// Vào Firebase Console → Realtime Database
// Kiểm tra có data trong /locations/ không
```

---

---

## 🧪 TEST BACKEND CONNECTION

### **Test 1: Backend có chạy không?**
```bash
# Mở browser, truy cập:
http://localhost:5224/scalar

# Hoặc test API trực tiếp:
curl http://localhost:5224/api/Health
```

### **Test 2: Flutter có kết nối được backend không?**
```dart
// Thêm method test vào ApiService
Future<bool> testConnection() async {
  try {
    final response = await http.get(Uri.parse('$baseUrl/Health'));
    print('Backend response: ${response.statusCode}');
    return response.statusCode == 200;
  } catch (e) {
    print('Connection error: $e');
    return false;
  }
}

// Gọi trong initState
bool connected = await apiService.testConnection();
print('Backend connected: $connected');
```

### **Test 3: JWT Token có hợp lệ không?**
```dart
// Sau khi login, test lấy user info
final user = await apiService.getCurrentUser();
if (user != null) {
  print('✅ Token hợp lệ, user: ${user['email']}');
} else {
  print('❌ Token không hợp lệ hoặc đã hết hạn');
}
```

### **Test 4: Firebase có hoạt động không?**
```dart
// Test write
await FirebaseDatabase.instance.ref().child('test').set({
  'message': 'Hello Firebase',
  'timestamp': DateTime.now().millisecondsSinceEpoch,
});

// Test read
final snapshot = await FirebaseDatabase.instance.ref().child('test').get();
print('Firebase data: ${snapshot.value}');
```

---

## 🐛 TROUBLESHOOTING

### **Lỗi: "Connection refused" / "Network error"**

**Nguyên nhân:** Flutter không kết nối được backend

**Giải pháp:**
```dart
// 1. Kiểm tra backend có chạy không
// Mở browser: http://localhost:5224/scalar

// 2. Kiểm tra baseUrl đúng chưa
// Android Emulator:
static const String baseUrl = 'http://10.0.2.2:5224/api';

// iOS Simulator:
static const String baseUrl = 'http://localhost:5224/api';

// Real Device (cùng WiFi):
// Lấy IP máy: ipconfig (Windows) hoặc ifconfig (Mac/Linux)
static const String baseUrl = 'http://192.168.1.X:5224/api';

// 3. Tắt firewall tạm thời (Windows)
// Settings → Windows Security → Firewall → Allow app
```

### **Lỗi: "401 Unauthorized"**

**Nguyên nhân:** JWT token không hợp lệ hoặc chưa login

**Giải pháp:**
```dart
// 1. Kiểm tra đã login chưa
final token = await SharedPreferences.getInstance().getString('jwt_token');
print('Token: $token');

// 2. Token có trong header chưa
headers: {
  'Authorization': 'Bearer $token', // ⚠️ Phải có "Bearer " phía trước
  'Content-Type': 'application/json',
}

// 3. Login lại
await apiService.login('user@example.com', 'password');
```

### **Lỗi: "MissingPluginException"**
```bash
flutter clean
flutter pub get
cd android && ./gradlew clean && cd ..
flutter run
```

### **Lỗi: "Location permission denied"**
- Android: Settings → Apps → Your App → Permissions → Location → Allow
- iOS: Settings → Privacy → Location Services → Your App → Allow

### **Lỗi: "Firebase not initialized"**
```bash
flutterfire configure
```

### **Lỗi: "Google Maps not showing"**
- Kiểm tra API key trong AndroidManifest.xml
- Enable Maps SDK trong Google Cloud Console

---

## 📋 CHECKLIST ĐẢM BẢO KHÔNG LỖI

### **Backend:**
- [ ] ✅ Backend đang chạy (`dotnet run`)
- [ ] ✅ Test API: `http://localhost:5224/scalar` mở được
- [ ] ✅ Có user để test login (email + password)
- [ ] ✅ Firewall allow port 5224 (nếu test trên real device)

### **Flutter:**
- [ ] ✅ `flutter pub get` chạy thành công
- [ ] ✅ `baseUrl` trong `api_service.dart` đúng môi trường
- [ ] ✅ Test connection: `await apiService.testConnection()` return `true`

### **Firebase:**
- [ ] ✅ Firebase project đã tạo
- [ ] ✅ `google-services.json` (Android) và `GoogleService-Info.plist` (iOS) đã thêm
- [ ] ✅ `flutterfire configure` đã chạy
### **Khi mở app lần đầu:**
1. ✅ Xin quyền location → User cho phép
2. ✅ Login screen hiện ra (hoặc tự động login nếu có token)
3. ✅ Sau khi login thành công → chuyển đến MapScreen

### **Trong MapScreen:**
4. ✅ Map hiển thị vị trí hiện tại của bạn
5. ✅ "My location" button bật (chấm xanh trên map)
6. ✅ Vị trí tự động upload lên Firebase mỗi 20m di chuyển
7. ✅ Console log: `📍 Uploaded location: ...`

### **Thêm user vào watchlist:**
8. ✅ Tap FAB (+) button → Gọi `addToWatchlist(456)`
9. ✅ Backend lưu vào database
10. ✅ Flutter bắt đầu listen location của user 456 từ Firebase
11. ✅ Snackbar hiện: "Đã thêm user 456 vào watchlist"

### **Real-time tracking:**
12. ✅ Marker màu xanh hiện vị trí của user 456
13. ✅ User 456 di chuyển → marker update real-time
14. ✅ Console log: Location updates từ Firebase

### **Khi thoát app:**
15. ✅ `dispose()` được gọi
16. ✅ GPS stream bị stop
17. ✅ Vị trí của bạn bị xóa khỏi Firebase (`/locations/yourUid`)
18. ✅ Tất cả listeners bị cancel

---

## 📊 DEBUG LOGS MẪU

### **Khi app chạy thành công:**
```
✅ Firebase initialized
✅ Location permission granted
📍 Current position: 10.762622, 106.660172
✅ Started uploading location
✅ Loaded watchlist: [123, 456, 789]
👂 Listening to user 123
👂 Listening to user 456
👂 Listening to user 789
📍 Uploaded location: 10.762622, 106.660172
📍 Received location for user 456: 10.123, 106.456
```

### **Khi có lỗi:**
```
❌ Backend response: 401 (Token không hợp lệ - cần login lại)
❌ Connection error: SocketException (Backend không chạy hoặc URL sai)
❌ Location permission denied (User từ chối quyền)
❌ Firebase error: Permission denied (Security rules sai)
```
- [ ] ✅ Maps SDK for Android đã enable
- [ ] ✅ Maps SDK for iOS đã enable
- [ ] ✅ API key đã thêm vào AndroidManifest.xml

### **Permissions:**
- [ ] ✅ Location permissions đã thêm vào AndroidManifest.xml
- [ ] ✅ Location permissions đã thêm vào Info.plist (iOS)
- [ ] ✅ App đã xin quyền location khi chạy
- [ ] ✅ minSdkVersion >= 21 (Android)

---

## 🎯 KẾT QUẢ MONG ĐỢI

1. ✅ Mở app → xin quyền location → cho phép
2. ✅ Map hiển thị vị trí hiện tại
3. ✅ Vị trí tự động upload lên Firebase mỗi 20m di chuyển
4. ✅ Tap FAB (+) → thêm user vào watchlist
5. ✅ Marker của user khác hiển thị real-time trên map
6. ✅ Thoát app → location bị xóa khỏi Firebase

---

## 📞 SUPPORT

**Lỗi không giải quyết được?**
1. Check logs: `flutter run -v`
2. Check Firebase Console → Realtime Database → Data
3. Check backend logs
4. Verify API endpoint: `http://10.0.2.2:5224/scalar`

---

**🎉 DONE! Làm theo từng bước, đảm bảo không lỗi!**
