namespace capstone_backend.Business.DTOs.Advertisement;

public class RejectAdvertisementRequest
{
    public int AdvertisementId { get; set; }
    public string? Reason { get; set; }
}
