using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AeFinder.ApiKeys;

public class SetAuthorisedDomainInput: IValidatableObject
{
    [MinLength(1), MaxLength(50)] 
    public List<string> Domains { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var domain in Domains)
        {
            if (domain.IsNullOrWhiteSpace())
            {
                yield return new ValidationResult("Authorised domain cannot be empty.");
            }
        }
    }
}