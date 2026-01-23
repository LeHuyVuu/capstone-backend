namespace capstone_backend.Api.Models
{
    public interface ICurrentUser
    {
        int? UserId { get; }
        string? Email { get; }
        string? Role { get; }
        IReadOnlyDictionary<string, string>? Claims { get; }
    }
}
