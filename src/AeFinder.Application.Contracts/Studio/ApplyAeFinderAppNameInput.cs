using System.ComponentModel.DataAnnotations;

namespace AeFinder.Studio;

public class ApplyAeFinderAppNameInput
{
    [Required] public string Name { get; set; }
    [Required] public string AppId { get; set; }
}