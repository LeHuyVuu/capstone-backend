using capstone_backend.Business.DTOs.Admin;
using capstone_backend.Business.DTOs.TestType;
using capstone_backend.Business.Interfaces;
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

        public AdminController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> GetRoleAdmin()
        {
            if (GetCurrentUserRole() != "ADMIN")
                return ForbiddenResponse("You do not have permission to access this resource");
            else 
                return OkResponse("You are an admin");
        }

        [HttpGet("dashboard")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetDashboard([FromQuery] int days = 30)
        {
            var now = DateTime.UtcNow;
            var startDate = now.AddDays(-days);

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
                .Where(c => c.IsDeleted != true && c.Status == "ACTIVE")
                .CountAsync();

            var totalTransactions = await _unitOfWork.Context.Set<Data.Entities.Transaction>()
                .CountAsync();

            var totalRevenue = await _unitOfWork.Context.Set<Data.Entities.Transaction>()
                .Where(t => t.Status == "SUCCESS")
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var totalReports = await _unitOfWork.Context.Set<Data.Entities.Report>()
                .CountAsync();

            var totalPosts = await _unitOfWork.Context.Set<Data.Entities.Post>()
                .Where(p => p.IsDeleted != true)
                .CountAsync();

            var totalAdsOrders = await _unitOfWork.Context.Set<Data.Entities.AdsOrder>()
                .CountAsync();

            var activeAdsOrders = await _unitOfWork.Context.Set<Data.Entities.AdsOrder>()
                .Where(a => a.Status == "ACTIVE")
                .CountAsync();

            var totalMemberSubscriptions = await _unitOfWork.Context.Set<Data.Entities.MemberSubscriptionPackage>()
                .CountAsync();

            var activeMemberSubscriptions = await _unitOfWork.Context.Set<Data.Entities.MemberSubscriptionPackage>()
                .Where(m => m.Status == "ACTIVE" && m.EndDate >= DateTime.UtcNow)
                .CountAsync();

            var totalVenueSubscriptions = await _unitOfWork.Context.Set<Data.Entities.VenueSubscriptionPackage>()
                .CountAsync();

            var activeVenueSubscriptions = await _unitOfWork.Context.Set<Data.Entities.VenueSubscriptionPackage>()
                .Where(v => v.Status == "ACTIVE" && v.EndDate >= DateTime.UtcNow)
                .CountAsync();

            var userGrowth = await _unitOfWork.Context.Set<Data.Entities.UserAccount>()
                .Where(u => u.CreatedAt >= startDate && u.IsDeleted != true)
                .GroupBy(u => u.CreatedAt.HasValue ? u.CreatedAt.Value.Date : DateTime.MinValue)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var revenueByDay = await _unitOfWork.Context.Set<Data.Entities.Transaction>()
                .Where(t => t.CreatedAt >= startDate && t.Status == "SUCCESS")
                .GroupBy(t => t.CreatedAt.HasValue ? t.CreatedAt.Value.Date : DateTime.MinValue)
                .Select(g => new { Date = g.Key, Amount = g.Sum(t => t.Amount) })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var transactionsByDay = await _unitOfWork.Context.Set<Data.Entities.Transaction>()
                .Where(t => t.CreatedAt >= startDate)
                .GroupBy(t => t.CreatedAt.HasValue ? t.CreatedAt.Value.Date : DateTime.MinValue)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var venueGrowth = await _unitOfWork.Context.Set<Data.Entities.VenueLocation>()
                .Where(v => v.CreatedAt >= startDate && v.IsDeleted != true)
                .GroupBy(v => v.CreatedAt.HasValue ? v.CreatedAt.Value.Date : DateTime.MinValue)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var postActivity = await _unitOfWork.Context.Set<Data.Entities.Post>()
                .Where(p => p.CreatedAt >= startDate && p.IsDeleted != true)
                .GroupBy(p => p.CreatedAt.HasValue ? p.CreatedAt.Value.Date : DateTime.MinValue)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToListAsync();

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
                UserGrowthChart = userGrowth.Select(x => new ChartDataPoint 
                { 
                    Label = x.Date.ToString("yyyy-MM-dd"), 
                    Value = x.Count 
                }).ToList(),
                RevenueChart = revenueByDay.Select(x => new ChartDataPoint 
                { 
                    Label = x.Date.ToString("yyyy-MM-dd"), 
                    Value = x.Amount 
                }).ToList(),
                TransactionChart = transactionsByDay.Select(x => new ChartDataPoint 
                { 
                    Label = x.Date.ToString("yyyy-MM-dd"), 
                    Value = x.Count 
                }).ToList(),
                VenueGrowthChart = venueGrowth.Select(x => new ChartDataPoint 
                { 
                    Label = x.Date.ToString("yyyy-MM-dd"), 
                    Value = x.Count 
                }).ToList(),
                PostActivityChart = postActivity.Select(x => new ChartDataPoint 
                { 
                    Label = x.Date.ToString("yyyy-MM-dd"), 
                    Value = x.Count 
                }).ToList()
            };

            return OkResponse(dashboard);
        }
    }
}
