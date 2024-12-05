using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AeFinder.ApiKeys;

public class CreateApiKeyInput: IValidatableObject
{
    [MinLength(1),MaxLength(30)]
    public string Name { get; set; }
    public bool IsEnableSpendingLimit { get; set; }
    public decimal SpendingLimitUsdt { get; set; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SpendingLimitUsdt < 0)
        {
            yield return new ValidationResult("Invalid SpendingLimitUsdt.");
        }
    }
}