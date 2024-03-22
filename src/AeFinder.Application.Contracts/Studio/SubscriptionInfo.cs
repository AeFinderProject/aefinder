using System.ComponentModel.DataAnnotations;

namespace AeFinder.Studio;

public class SubscriptionInfo
{
    [Required] public string AppId { get; set; }
    public string SubscriptionManifest { get; set; }
    public byte[] AppDll { get; set; }
}