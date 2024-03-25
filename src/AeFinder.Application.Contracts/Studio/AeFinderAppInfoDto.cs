using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AeFinder.Studio;

public class AeFinderAppInfoDto : AeFinderAppInfo
{
}

public class AeFinderAppInfo : IValidatableObject
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string LogoUrl { get; set; }
    public string SourceCodeUrl { get; set; }
    public string AppId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(AppId))
        {
            yield return new ValidationResult("Invalid AppId input.");
        }

        if (string.IsNullOrEmpty(Name))
        {
            yield return new ValidationResult("Invalid Name input.");
        }
    }
}