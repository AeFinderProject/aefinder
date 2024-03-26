using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AeFinder.Studio;

public class QueryAeFinderAppInput : IValidatableObject
{
    [Required] public string AppId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(AppId))
        {
            yield return new ValidationResult("Invalid AppId input.");
        }
    }
}