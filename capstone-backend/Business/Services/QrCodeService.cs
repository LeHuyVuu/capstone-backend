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

            //var instagramGradient = new GradientOptions([
            //    SKColor.Parse("#111111"), // near black
            //    SKColor.Parse("#2B2D42"), // dark indigo gray
            //    SKColor.Parse("#3A0CA3"), // deep purple
            //    SKColor.Parse("#1D3557"), // dark blue
            //    SKColor.Parse("#000000")  // black
            //],
            //GradientDirection.TopLeftToBottomRight,
            //[0f, 0.25f, 0.5f, 0.75f, 1f]);

            using var logo = SKBitmap.Decode(File.ReadAllBytes(logoPath));
            var icon = IconData.FromImage(logo, iconSizePercent: 14, iconBorderWidth: 6);

            //var qrBuilder = new QRCodeImageBuilder(content)
            //    .WithSize(1024, 1024)
            //    .WithErrorCorrection(ECCLevel.H)
            //    .WithQuietZone(4)
            //    .WithColors(backgroundColor: SKColors.White, clearColor: SKColors.White)
            //    .WithModuleShape(CircleModuleShape.Default, sizePercent: 0.95f)
            //    .WithFinderPatternShape(RoundedRectangleCircleFinderPatternShape.Default)
            //    .WithGradient(instagramGradient)
            //    .WithIcon(icon);

            var qrBuilder = new QRCodeImageBuilder(content)
                .WithSize(800, 800)
                .WithErrorCorrection(ECCLevel.H) // H recommended for icons
                .WithColors(codeColor: SKColor.Parse("#1F1F1F"), backgroundColor: SKColors.White)
                .WithIcon(icon);

            var pngBytes = qrBuilder.ToByteArray();
            return pngBytes;
        }
    }
}
