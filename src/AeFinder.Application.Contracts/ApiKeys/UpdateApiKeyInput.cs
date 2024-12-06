using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Orleans;

namespace AeFinder.ApiKeys;

[GenerateSerializer]
public class UpdateApiKeyInput: IValidatableObject
{
    [MinLength(1),MaxLength(30)]
    public string Name { get; set; }
    public bool? IsEnableSpendingLimit { get; set; }
    public decimal? SpendingLimitUsdt { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SpendingLimitUsdt.HasValue && SpendingLimitUsdt.Value < 0)
        {
            yield return new ValidationResult("Invalid SpendingLimitUsdt.");
        }
    }
}