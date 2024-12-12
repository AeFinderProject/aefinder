using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Orleans;

namespace AeFinder.ApiKeys;

[GenerateSerializer]
public class CreateApiKeyInput: IValidatableObject
{
    [MinLength(1),MaxLength(30)]
    [Id(0)]public string Name { get; set; }
    [Id(1)]public bool IsEnableSpendingLimit { get; set; }
    [Id(2)]public decimal SpendingLimitUsdt { get; set; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SpendingLimitUsdt < 0)
        {
            yield return new ValidationResult("The spending limit cannot be less than 0.");
        }
    }
}