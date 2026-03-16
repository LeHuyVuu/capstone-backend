using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Wallet;

public class CreateWithdrawRequestRequest
{
    [Required]
    public decimal Amount { get; set; }
    
    [Required]
    public BankInfoDto BankInfo { get; set; } = null!;
}

public class BankInfoDto
{
    [Required]
    public string BankName { get; set; } = null!;
    
    [Required]
    public string AccountNumber { get; set; } = null!;
    
    [Required]
    public string AccountName { get; set; } = null!;
}
