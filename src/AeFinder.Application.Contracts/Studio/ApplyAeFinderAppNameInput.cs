using System.ComponentModel.DataAnnotations;

namespace AeFinder.Studio;

public class ApplyAeFinderAppNameInput
{
    [Required] public string Name { get; set; }

    
}