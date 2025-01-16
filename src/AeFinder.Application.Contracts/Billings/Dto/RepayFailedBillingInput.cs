using System.ComponentModel.DataAnnotations;

namespace AeFinder.Billings.Dto;

public class RepayFailedBillingInput
{
    [Required]
    public string OrganizationId { get; set; }
    [Required]
    public string BillingId { get; set; }
}