using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [ApiController]
    public class PaymentRedirectController : ControllerBase
    {
        [HttpGet("/vnpay-return")]
        [AllowAnonymous]
        public IActionResult VNPayReturn()
        {
            var responseCode = Request.Query["vnp_ResponseCode"].ToString();
            bool isSuccess = responseCode == "00";

            var title = isSuccess ? "Thanh toán thành công!" : "Giao dịch thất bại!";
            var message = isSuccess
                ? "Tuyệt vời! Giao dịch của bạn đã hoàn tất."
                : "Có lỗi xảy ra hoặc bạn đã hủy giao dịch.";

            var iconHtml = isSuccess
                ? @"<div class='mx-auto flex items-center justify-center h-20 w-20 rounded-full bg-green-100 mb-6'>
                        <svg class='h-10 w-10 text-green-600' fill='none' stroke='currentColor' viewBox='0 0 24 24'>
                            <path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M5 13l4 4L19 7'></path>
                        </svg>
                    </div>"
                : @"<div class='mx-auto flex items-center justify-center h-20 w-20 rounded-full bg-red-100 mb-6'>
                        <svg class='h-10 w-10 text-red-600' fill='none' stroke='currentColor' viewBox='0 0 24 24'>
                            <path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M6 18L18 6M6 6l12 12'></path>
                        </svg>
                    </div>";

            var html = $@"
            <!DOCTYPE html>
            <html lang='vi'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>CoupleMood - Kết quả thanh toán</title>
                <script src='https://cdn.tailwindcss.com'></script>
                <style>
                    .loader {{ border-top-color: #3b82f6; animation: spinner 1.5s linear infinite; }}
                    @keyframes spinner {{ 0% {{ transform: rotate(0deg); }} 100% {{ transform: rotate(360deg); }} }}
                </style>
            </head>
            <body class='bg-gray-50 h-screen flex items-center justify-center font-sans px-4'>
                <div class='bg-white p-8 rounded-3xl shadow-xl max-w-sm w-full text-center border border-gray-100'>
                    
                    {iconHtml}

                    <h2 class='text-2xl font-extrabold text-gray-800 mb-2'>{title}</h2>
                    <p class='text-gray-500 mb-8 text-sm'>{message}</p>
                    
                    <div class='flex flex-col items-center justify-center'>
                        <div class='loader ease-linear rounded-full border-4 border-gray-200 h-8 w-8 mb-3'></div>
                        <p class='text-xs text-gray-400'>Đang đóng giao dịch...</p>
                    </div>
                </div>
            </body>
            </html>";

            return Content(html, "text/html; charset=utf-8");
        }
    }
}
