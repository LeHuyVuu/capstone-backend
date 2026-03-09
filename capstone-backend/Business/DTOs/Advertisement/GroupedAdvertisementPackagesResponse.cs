namespace capstone_backend.Business.DTOs.Advertisement;

public class GroupedAdvertisementPackagesResponse
{
    public Dictionary<string, List<AdvertisementPackageResponse>> Data { get; set; } = new();
}
