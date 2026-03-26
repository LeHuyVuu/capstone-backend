using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Wallet;

public class RejectWithdrawRequestRequest
{
    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = null!;
}
