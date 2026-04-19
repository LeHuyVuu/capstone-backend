using capstone_backend.Business.DTOs.Admin;
using capstone_backend.Business.DTOs.TestType;
using capstone_backend.Business.DTOs.Wallet;
using capstone_backend.Business.DTOs.VenueLocation;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Services;
using capstone_backend.Data.Enums;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AdminController : BaseController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly WalletService _walletService;
        private readonly IVenueLocationService _venueLocationService;

        public AdminController(IUnitOfWork unitOfWork, WalletService walletService, IVenueLocationService venueLocationService)
        {
            _unitOfWork = unitOfWork;
            _walletService = walletService;
            _venueLocationService = venueLocationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRoleAdmin()
        {
            if (GetCurrentUserRole() != "ADMIN")
                return ForbiddenResponse("Bạn không có quyền truy cập tài nguyên này");
            else
                return OkResponse("Bạn là quản trị viên");
        }

        [HttpGet("dashboard")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetDashboard([FromQuery] AdminDashboardRequest request)
        {
            var now = DateTime.UtcNow;
            DateTime calculatedStartDate;
            DateTime calculatedEndDate;
            string period;

            try
            {
                // Xác định period và tính toán startDate/endDate dựa trên tham số
                if (request.Day.HasValue && request.Month.HasValue && request.Year.HasValue)
                {
                    // Lọc theo ngày cụ thể
                    period = "day";
                    calculatedStartDate = new DateTime(request.Year.Value, request.Month.Value, request.Day.Value, 0, 0, 0, DateTimeKind.Utc);
                    calculatedEndDate = calculatedStartDate.AddDays(1).AddSeconds(-1);
                }
                else if (request.Month.HasValue && request.Year.HasValue)
                {
                    // Lọc theo tháng cụ thể
                    period = "month";
                    calculatedStartDate = new DateTime(request.Year.Value, request.Month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
                    calculatedEndDate = calculatedStartDate.AddMonths(1).AddSeconds(-1);
                }
                else if (request.Year.HasValue)
                {
                    // Lọc theo năm cụ thể
                    period = "year";
                    calculatedStartDate = new DateTime(request.Year.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    calculatedEndDate = calculatedStartDate.AddYears(1).AddSeconds(-1);
                }
                else
                {
                    // Mặc định: 30 ngày gần nhất
                    period = "month";
                    calculatedStartDate = now.AddDays(-30);
                    calculatedEndDate = now;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                return BadRequestResponse("Ngày không hợp lệ. Vui lòng kiểm tra lại ngày, tháng và năm");
            }
            catch (Exception ex)
            {
                return BadRequestResponse($"Lỗi khi xử lý tham số ngày: {ex.Message}");
            }

            var totalUsers = await _unitOfWork.Context.Set<Data.Entities.UserAccount>()
                .Where(u => u.IsDeleted != true)
                .CountAsync();

            var totalVenueOwnerProfiles = await _unitOfWork.Context.Set<Data.Entities.VenueOwnerProfile>()
                .Where(v => v.IsDeleted != true)
                .CountAsync();

            var totalVenueLocations = await _unitOfWork.Context.Set<Data.Entities.VenueLocation>()
                .Where(v => v.IsDeleted != true)
                .CountAsync();

            var totalMemberProfiles = await _unitOfWork.Context.Set<Data.Entities.MemberProfile>()
                .Where(m => m.IsDeleted != true)
                .CountAsync();

            var activeCouples = await _unitOfWork.Context.Set<Data.Entities.CoupleProfile>()
                .Where(c => c.IsDeleted != true && c.Status == CoupleProfileStatus.ACTIVE.ToString())
                .CountAsync();

            var totalTransactions = await _unitOfWork.Context.Set<Data.Entities.Transaction>()
                .Where(t => t.Status == TransactionStatus.SUCCESS.ToString()
                    && (t.TransType == (int)TransactionType.VENUE_SUBSCRIPTION
                        || t.TransType == (int)TransactionType.ADS_ORDER
                        || t.TransType == (int)TransactionType.MEMBER_SUBSCRIPTION))
                .CountAsync();

            var totalRevenue = await _unitOfWork.Context.Set<Data.Entities.Transaction>()
                .Where(t => t.Status == TransactionStatus.SUCCESS.ToString()
                    && (t.TransType == (int)TransactionType.VENUE_SUBSCRIPTION
                        || t.TransType == (int)TransactionType.ADS_ORDER
                        || t.TransType == (int)TransactionType.MEMBER_SUBSCRIPTION))
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var totalReports = await _unitOfWork.Context.Set<Data.Entities.Report>()
                .CountAsync();

            var totalPosts = await _unitOfWork.Context.Set<Data.Entities.Post>()
                .Where(p => p.IsDeleted != true)
                .CountAsync();

            var totalAdsOrders = await _unitOfWork.Context.Set<Data.Entities.AdsOrder>()
                .CountAsync();

            var activeAdsOrders = await _unitOfWork.Context.Set<Data.Entities.AdsOrder>()
                .Where(a => a.Status == AdsOrderStatus.COMPLETED.ToString())
                .CountAsync();

            var totalMemberSubscriptions = await _unitOfWork.Context.Set<Data.Entities.MemberSubscriptionPackage>()
                .CountAsync();

            var activeMemberSubscriptions = await _unitOfWork.Context.Set<Data.Entities.MemberSubscriptionPackage>()
                .Where(m => m.Status == MemberSubscriptionPackageStatus.ACTIVE.ToString() && m.EndDate >= DateTime.UtcNow)
                .CountAsync();

            var totalVenueSubscriptions = await _unitOfWork.Context.Set<Data.Entities.VenueSubscriptionPackage>()
                .CountAsync();

            var activeVenueSubscriptions = await _unitOfWork.Context.Set<Data.Entities.VenueSubscriptionPackage>()
                .Where(v => v.Status == VenueSubscriptionPackageStatus.ACTIVE.ToString() && v.EndDate >= DateTime.UtcNow)
                .CountAsync();

            // Group data theo period
            var userGrowth = await GroupDataByPeriod(
                _unitOfWork.Context.Set<Data.Entities.UserAccount>()
                    .Where(u => u.CreatedAt >= calculatedStartDate && u.CreatedAt <= calculatedEndDate && u.IsDeleted != true),
                period);

            var revenueByPeriod = await GroupTransactionsByPeriod(
                _unitOfWork.Context.Set<Data.Entities.Transaction>()
                    .Where(t => t.CreatedAt >= calculatedStartDate 
                        && t.CreatedAt <= calculatedEndDate 
                        && t.Status == TransactionStatus.SUCCESS.ToString()
                        && (t.TransType == (int)TransactionType.VENUE_SUBSCRIPTION
                            || t.TransType == (int)TransactionType.ADS_ORDER
                            || t.TransType == (int)TransactionType.MEMBER_SUBSCRIPTION)),
                period);

            var transactionsByPeriod = await GroupDataByPeriod(
                _unitOfWork.Context.Set<Data.Entities.Transaction>()
                    .Where(t => t.CreatedAt >= calculatedStartDate && t.CreatedAt <= calculatedEndDate),
                period);

            var venueGrowth = await GroupDataByPeriod(
                _unitOfWork.Context.Set<Data.Entities.VenueLocation>()
                    .Where(v => v.CreatedAt >= calculatedStartDate
                        && v.CreatedAt <= calculatedEndDate
                        && v.IsDeleted != true
                        && v.Status == VenueLocationStatus.ACTIVE.ToString()),
                period);

            var postActivity = await GroupDataByPeriod(
                _unitOfWork.Context.Set<Data.Entities.Post>()
                    .Where(p => p.CreatedAt >= calculatedStartDate
                        && p.CreatedAt <= calculatedEndDate
                        && p.IsDeleted != true
                        && p.Status == PostStatus.PUBLISHED.ToString()),
                period);

            var dashboard = new AdminDashboardResponse
            {
                TotalUsers = totalUsers,
                TotalVenueOwnerProfiles = totalVenueOwnerProfiles,
                TotalVenueLocations = totalVenueLocations,
                TotalMemberProfiles = totalMemberProfiles,
                ActiveCouples = activeCouples,
                TotalTransactions = totalTransactions,
                TotalRevenue = totalRevenue,
                TotalReports = totalReports,
                TotalPosts = totalPosts,
                TotalAdsOrders = totalAdsOrders,
                ActiveAdsOrders = activeAdsOrders,
                TotalMemberSubscriptions = totalMemberSubscriptions,
                ActiveMemberSubscriptions = activeMemberSubscriptions,
                TotalVenueSubscriptions = totalVenueSubscriptions,
                ActiveVenueSubscriptions = activeVenueSubscriptions,
                UserGrowthChart = userGrowth,
                RevenueChart = revenueByPeriod,
                TransactionChart = transactionsByPeriod,
                VenueGrowthChart = venueGrowth,
                PostActivityChart = postActivity,
                Period = period,
                StartDate = calculatedStartDate,
                EndDate = calculatedEndDate
            };

            return OkResponse(dashboard);
        }

        private async Task<List<ChartDataPoint>> GroupDataByPeriod<T>(IQueryable<T> query, string period) where T : class
        {
            var createdAtProperty = typeof(T).GetProperty("CreatedAt");
            if (createdAtProperty == null)
                return new List<ChartDataPoint>();

            var data = await query.ToListAsync();

            var grouped = period.ToLower() switch
            {
                "day" => data
                    .GroupBy(x => {
                        var createdAt = createdAtProperty.GetValue(x) as DateTime?;
                        return createdAt?.ToString("yyyy-MM-dd HH:00") ?? "Unknown";
                    })
                    .Select(g => new ChartDataPoint { Label = g.Key, Value = g.Count() })
                    .OrderBy(x => x.Label)
                    .ToList(),
                
                "week" => data
                    .GroupBy(x => {
                        var createdAt = createdAtProperty.GetValue(x) as DateTime?;
                        return createdAt?.ToString("yyyy-MM-dd") ?? "Unknown";
                    })
                    .Select(g => new ChartDataPoint { Label = g.Key, Value = g.Count() })
                    .OrderBy(x => x.Label)
                    .ToList(),
                
                "month" => data
                    .GroupBy(x => {
                        var createdAt = createdAtProperty.GetValue(x) as DateTime?;
                        return createdAt?.ToString("yyyy-MM-dd") ?? "Unknown";
                    })
                    .Select(g => new ChartDataPoint { Label = g.Key, Value = g.Count() })
                    .OrderBy(x => x.Label)
                    .ToList(),
                
                "year" => data
                    .GroupBy(x => {
                        var createdAt = createdAtProperty.GetValue(x) as DateTime?;
                        return createdAt?.ToString("yyyy-MM") ?? "Unknown";
                    })
                    .Select(g => new ChartDataPoint { Label = g.Key, Value = g.Count() })
                    .OrderBy(x => x.Label)
                    .ToList(),
                
                _ => data
                    .GroupBy(x => {
                        var createdAt = createdAtProperty.GetValue(x) as DateTime?;
                        return createdAt?.ToString("yyyy-MM-dd") ?? "Unknown";
                    })
                    .Select(g => new ChartDataPoint { Label = g.Key, Value = g.Count() })
                    .OrderBy(x => x.Label)
                    .ToList()
            };

            return grouped;
        }

        private async Task<List<ChartDataPoint>> GroupTransactionsByPeriod(IQueryable<Data.Entities.Transaction> query, string period)
        {
            var data = await query.ToListAsync();

            var grouped = period.ToLower() switch
            {
                "day" => data
                    .GroupBy(x => x.CreatedAt?.ToString("yyyy-MM-dd HH:00") ?? "Unknown")
                    .Select(g => new ChartDataPoint { Label = g.Key, Value = g.Sum(t => t.Amount) })
                    .OrderBy(x => x.Label)
                    .ToList(),
                
                "week" => data
                    .GroupBy(x => x.CreatedAt?.ToString("yyyy-MM-dd") ?? "Unknown")
                    .Select(g => new ChartDataPoint { Label = g.Key, Value = g.Sum(t => t.Amount) })
                    .OrderBy(x => x.Label)
                    .ToList(),
                
                "month" => data
                    .GroupBy(x => x.CreatedAt?.ToString("yyyy-MM-dd") ?? "Unknown")
                    .Select(g => new ChartDataPoint { Label = g.Key, Value = g.Sum(t => t.Amount) })
                    .OrderBy(x => x.Label)
                    .ToList(),
                
                "year" => data
                    .GroupBy(x => x.CreatedAt?.ToString("yyyy-MM") ?? "Unknown")
                    .Select(g => new ChartDataPoint { Label = g.Key, Value = g.Sum(t => t.Amount) })
                    .OrderBy(x => x.Label)
                    .ToList(),
                
                _ => data
                    .GroupBy(x => x.CreatedAt?.ToString("yyyy-MM-dd") ?? "Unknown")
                    .Select(g => new ChartDataPoint { Label = g.Key, Value = g.Sum(t => t.Amount) })
                    .OrderBy(x => x.Label)
                    .ToList()
            };

            return grouped;
        }
        
        /// <summary>
        /// Lấy tất cả transactions cho admin với pagination và filters
        /// </summary>
        [HttpGet("transactions")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetAllTransactions(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] int? transType = null,
            [FromQuery] int? userId = null)
        {
            try
            {
                var result = await _walletService.GetAllTransactionsForAdminAsync(
                    pageNumber, 
                    pageSize, 
                    status, 
                    transType, 
                    userId);

                return OkResponse(result);
            }
            catch (Exception ex)
            {
                return BadRequestResponse($"Lỗi khi lấy danh sách giao dịch: {ex.Message}");
            }
        }

        [HttpPatch("venues/{id}/status")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> ChangeVenueStatus([FromRoute] int id, [FromBody] ChangeVenueStatusByAdminRequest request)
        {
            var adminUserId = GetCurrentUserId();
            if (!adminUserId.HasValue)
            {
                return UnauthorizedResponse("Quản trị viên chưa được xác thực");
            }

            try
            {
                var result = await _venueLocationService.AdminChangeVenueStatusAsync(id, adminUserId.Value, request.Status, request.Reason);
                return OkResponse(result, $"Cập nhật trạng thái địa điểm sang {result.NewStatus} thành công");
            }
            catch (ArgumentException ex)
            {
                return BadRequestResponse(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequestResponse(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFoundResponse(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequestResponse($"Lỗi khi thay đổi trạng thái địa điểm: {ex.Message}");
            }
        }
    }
}
