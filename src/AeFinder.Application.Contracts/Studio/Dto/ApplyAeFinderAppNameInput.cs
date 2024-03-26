using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AeFinder.Studio;

public class ApplyAeFinderAppNameInput : IValidatableObject
{
    [Required] public string Name { get; set; }


    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(Name))
        {
            yield return new ValidationResult("Invalid Name input.");
        }
    }
}