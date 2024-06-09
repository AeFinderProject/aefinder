using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace AeFinder.BlockScan;

public class SubscriptionManifestDto : IValidatableObject
{
    [MinLength(1),MaxLength(10)]
    public List<SubscriptionDto> SubscriptionItems { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var chainIds = new HashSet<string>();
        var repeatedChainSubscription =
            SubscriptionItems.Where(subscription => !chainIds.Add(subscription.ChainId)).ToList();
        if (repeatedChainSubscription.Count > 0)
        {
            yield return new ValidationResult("The chain id is duplicated.");
        }
    }
}

public class SubscriptionDto
{
    [StringLength(maximumLength: 4, MinimumLength = 4)]
    public string ChainId { get; set; }
    [Range(1, long.MaxValue)] 
    public long StartBlockNumber { get; set; }
    public bool OnlyConfirmed { get; set; }
    public List<TransactionConditionDto> Transactions { get; set; } = new();
    public List<LogEventConditionDto> LogEvents { get; set; } = new();
}

public class TransactionConditionDto
{
    public string To { get; set; }
    public List<string> MethodNames { get; set; } = new();
}

public class LogEventConditionDto
{
    public string ContractAddress { get; set; }
    public List<string> EventNames { get; set; } = new();
}