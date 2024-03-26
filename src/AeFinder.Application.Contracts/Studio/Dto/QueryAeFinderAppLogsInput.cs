using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AeFinder.Studio;

public class QueryAeFinderAppLogsInput : IValidatableObject
{
    [Required] public string AppId { get; set; }
    public string Level { get; set; }
    public DateTime StartTime { get; set; } = DateTime.Now.AddMinutes(-5);
    public DateTime EndTime { get; set; } = DateTime.Now;
    public string Pod { get; set; }
    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 10;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(AppId))
        {
            yield return new ValidationResult("Invalid AppId input.");
        }

        if (SkipCount < 0)
        {
            yield return new ValidationResult("Invalid SkipCount input.");
        }

        var gap = MaxResultCount - SkipCount;
        if (gap < 1 || gap >= 100)
        {
            yield return new ValidationResult("Invalid MaxResultCount input.");
        }
    }
}