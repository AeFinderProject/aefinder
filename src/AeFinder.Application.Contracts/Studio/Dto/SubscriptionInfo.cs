using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace AeFinder.Studio;

public class SubscriptionInfo : IValidatableObject
{
    [Required] public string SubscriptionManifest { get; set; }
    [Required] public IFormFile AppDll { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(SubscriptionManifest))
        {
            yield return new ValidationResult("Invalid SubscriptionManifest input.");
        }

        if (AppDll == null || AppDll.Length == 0)
        {
            yield return new ValidationResult("Invalid AppDll input.");
        }

        var extension = Path.GetExtension(AppDll.FileName).ToLower();
        if (extension != ".dll")
        {
            yield return new ValidationResult("Invalid AppDll extension.");
        }
    }
}