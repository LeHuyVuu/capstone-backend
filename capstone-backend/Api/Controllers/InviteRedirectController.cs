using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

/// <summary>
/// Controller xử lý redirect deep link cho invite
/// </summary>
[ApiController]
public class InviteRedirectController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public InviteRedirectController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Redirect đến app hoặc hiển thị trang tải app
    /// URL: /invite/{code}
    /// </summary>
    [HttpGet("invite/{code}")]
    public IActionResult RedirectToApp(string code)
    {
        var devScheme = _configuration["DeepLink:DevScheme"] ?? "couplemood";
        var androidPackage = _configuration["DeepLink:AndroidPackageName"] ?? "com.example.couple_mood_mobile";
        var iosAppStoreId = _configuration["DeepLink:IOSAppStoreId"] ?? "";
        
        var deepLink = $"{devScheme}://invite?code={code}";
        var playStoreLink = $"https://play.google.com/store/apps/details?id={androidPackage}";
        var appStoreLink = string.IsNullOrEmpty(iosAppStoreId) 
            ? "https://apps.apple.com" 
            : $"https://apps.apple.com/app/id{iosAppStoreId}";

        // Detect user agent
        var userAgent = Request.Headers["User-Agent"].ToString().ToLower();
        var isIOS = userAgent.Contains("iphone") || userAgent.Contains("ipad");
        var isAndroid = userAgent.Contains("android");

        // HTML với auto-redirect - thử mở app, nếu không được thì chuyển thẳng đến store
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Đang mở CoupleMood...</title>
    <style>
        body {{
            margin: 0;
            padding: 20px;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
        }}
        .loader {{
            text-align: center;
            color: white;
        }}
        .spinner {{
            border: 4px solid rgba(255,255,255,0.3);
            border-top: 4px solid white;
            border-radius: 50%;
            width: 40px;
            height: 40px;
            animation: spin 1s linear infinite;
            margin: 0 auto 20px;
        }}
        @keyframes spin {{
            0% {{ transform: rotate(0deg); }}
            100% {{ transform: rotate(360deg); }}
        }}
    </style>
</head>
<body>
    <div class='loader'>
        <div class='spinner'></div>
        <p>Đang mở ứng dụng...</p>
    </div>
    <script>
        var deepLink = '{deepLink}';
        var storeLink = '{(isAndroid ? playStoreLink : appStoreLink)}';
        var isIOS = {isIOS.ToString().ToLower()};
        var isAndroid = {isAndroid.ToString().ToLower()};
        
        // Thử mở app
        window.location.href = deepLink;
        
        // Sau 1.5s, nếu vẫn ở trang này (app chưa cài) → chuyển đến store
        setTimeout(function() {{
            window.location.href = storeLink;
        }}, 1500);
        
        // Nếu app mở được, page sẽ bị ẩn đi
        document.addEventListener('visibilitychange', function() {{
            if (document.hidden) {{
                // App đã mở, không làm gì
            }}
        }});
    </script>
</body>
</html>";

        return Content(html, "text/html");
    }
}
