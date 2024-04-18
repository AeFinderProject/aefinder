using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using AeFinder.BlockScan;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace AeFinder.Studio;

public class SubscriptionInfo : IValidatableObject
{
    [Required] public IFormFile AppDll { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
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