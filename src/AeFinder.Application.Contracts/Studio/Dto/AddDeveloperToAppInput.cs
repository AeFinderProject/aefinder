using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AeFinder.Studio;

public class AddDeveloperToAppInput : IValidatableObject
{
    [Required] public string AppId { get; set; }
    [Required] public string DeveloperId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(AppId))
        {
            yield return new ValidationResult("Invalid AppId input.");
        }

        if (string.IsNullOrEmpty(DeveloperId))
        {
            yield return new ValidationResult("Invalid DeveloperId input.");
        }
    }
}