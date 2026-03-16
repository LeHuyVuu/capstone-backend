using capstone_backend.Business.Interfaces;
using SkiaSharp;
using SkiaSharp.QrCode;
using SkiaSharp.QrCode.Image;
using System.IO;

namespace capstone_backend.Business.Services
{
    public class QrCodeService : IQrCodeService
    {
        private readonly IWebHostEnvironment _env;

        public QrCodeService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public byte[] GenerateQrWithLogoAsync(string content)
        {
            var logoPath = Path.Combine(_env.WebRootPath, "assets", "logo.png");

            var instagramGradient = new GradientOptions([
                SKColor.Parse("FDC5F5"),  // Light Pink
                SKColor.Parse("F7AEF8"),  // Pink
                SKColor.Parse("B388EB"),  // Purple
                SKColor.Parse("8093F1"),  // Blue
                SKColor.Parse("72DDF7")   // Light Blue
            ],
            GradientDirection.TopLeftToBottomRight,
            [0f, 0.25f, 0.5f, 0.75f, 1f]);

            using var logo = SKBitmap.Decode(File.ReadAllBytes(logoPath));
            var icon = IconData.FromImage(logo, iconSizePercent: 14, iconBorderWidth: 9);

            var qrBuilder = new QRCodeImageBuilder(content)
                .WithSize(1024, 1024)
                .WithErrorCorrection(ECCLevel.H)
                .WithQuietZone(4)
                .WithColors(backgroundColor: SKColors.White, clearColor: SKColors.White)
                .WithModuleShape(CircleModuleShape.Default, sizePercent: 0.95f)
                .WithFinderPatternShape(RoundedRectangleCircleFinderPatternShape.Default)
                .WithGradient(instagramGradient)
                .WithIcon(icon);

            var pngBytes = qrBuilder.ToByteArray();
            return pngBytes;
        }
    }
}
