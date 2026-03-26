using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Wallet;

public class ApproveWithdrawRequestRequest
{
    [Required]
    public string Status { get; set; } = null!;

    [Required]
    public string ProofImageUrl { get; set; } = null!;
}
