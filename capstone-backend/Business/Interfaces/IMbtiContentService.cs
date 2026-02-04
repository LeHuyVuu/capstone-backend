using capstone_backend.Business.DTOs.PersonalityTest;

namespace capstone_backend.Business.Interfaces
{
    public interface IMbtiContentService
    {
        MbtiDetail GetResult(string mbtiCode);
    }
}
