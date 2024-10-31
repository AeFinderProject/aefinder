using System.ComponentModel.DataAnnotations;

namespace AeFinder.User.Dto;

public class BindUserWalletInput
{
    [Required]
    public string SignatureVal { get; set; }
    [Required]
    public string ChainId { get; set; }
    [Required]
    public string CaHash { get; set; }
    [Required]
    public long Timestamp { get; set; }
    /// <summary>
    /// EOA Wallet Address or CA Manager Address
    /// </summary>
    [Required]
    public string Address { get; set; }
}