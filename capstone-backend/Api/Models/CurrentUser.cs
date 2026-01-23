
using System.Security.Claims;

namespace capstone_backend.Api.Models
{
    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _accessor;

        public CurrentUser(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        private ClaimsPrincipal? User => _accessor.HttpContext?.User;
        public int? UserId =>
                int.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        public string? Email => User?.FindFirstValue(ClaimTypes.Email);
        public string? Role => User?.FindFirstValue(ClaimTypes.Role);

        public IReadOnlyDictionary<string, string> Claims =>
            (User?.Claims ?? Enumerable.Empty<Claim>())
            .GroupBy(c => c.Type)
            .ToDictionary(g => g.Key, g => g.Last().Value);
    }
}
