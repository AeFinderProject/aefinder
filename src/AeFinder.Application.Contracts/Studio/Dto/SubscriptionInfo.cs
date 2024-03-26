using System.ComponentModel.DataAnnotations;

namespace AeFinder.Studio;

public class SubscriptionInfo
{
    [Required] public string SubscriptionManifest { get; set; }
    [Required] public byte[] AppDll { get; set; }
}