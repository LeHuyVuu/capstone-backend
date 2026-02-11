namespace capstone_backend.Business.Interfaces
{
    public interface IModerationService
    {
        (bool IsValid, string? Message) CheckContent(string content);
    }
}
