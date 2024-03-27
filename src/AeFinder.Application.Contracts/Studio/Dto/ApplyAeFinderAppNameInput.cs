using System.ComponentModel.DataAnnotations;
using Volo.Abp.Validation;

namespace AeFinder.Studio;

public class ApplyAeFinderAppNameInput
{
    [Required]
    [DynamicStringLength(typeof(AppIdConsts), nameof(AppIdConsts.MaxNameLength))]
    [RegularExpression(AppIdConsts.NameRegex, ErrorMessage = "Invalid Name input.")]
    public string Name { get; set; }
}