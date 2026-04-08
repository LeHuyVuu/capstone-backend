using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("share")]
    [ApiController]
    public class ShareController : ControllerBase
    {
        [HttpGet("p/{code}")]
        public IActionResult SharePost([FromRoute] string code)
        {
            return GenerateShareHtml(
                title: "CoupleMood Post",
                description: "Xem bài viết này trên CoupleMood 💜",
                schemeUrl: $"couplemood://post/{code}"
            );
        }

        [HttpGet("c/{code}")]
        public IActionResult ShareCollection([FromRoute] string code)
        {
            return GenerateShareHtml(
                title: "CoupleMood Collection",
                description: "Khám phá bộ sưu tập này trên CoupleMood 💜",
                schemeUrl: $"couplemood://collection/{code}"
            );
        }

        private ContentResult GenerateShareHtml(string title, string description, string schemeUrl)
        {
            var imageUrl = "https://couplemood-store.s3.ap-southeast-2.amazonaws.com/system/logo.png";

            var html = $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='utf-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1' />

    <meta property='og:title' content='{title}' />
    <meta property='og:description' content='{description}' />
    <meta property='og:image' content='{imageUrl}' />
    <meta property='og:type' content='article' />

    <title>{title}</title>
    <script>
        window.location.href = '{schemeUrl}';
    </script>
</head>
<body>
    <p>{description}</p>
</body>
</html>";

            return Content(html, "text/html");
        }
    }
}
