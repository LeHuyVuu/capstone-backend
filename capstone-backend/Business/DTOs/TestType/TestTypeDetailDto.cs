namespace capstone_backend.Business.DTOs.TestType
{
    public class TestTypeDetailDto : TestTypeResponse
    {
        public List<VersionSummaryDto> Versions { get; set; }
        public int LastestVersion { get; set; }
    }
}
