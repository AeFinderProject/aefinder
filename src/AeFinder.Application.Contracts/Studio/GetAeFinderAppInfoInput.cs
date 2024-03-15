using System.ComponentModel.DataAnnotations;

namespace AeFinder.Studio;

public class GetAeFinderAppInfoInput
{
    [Required] public string Name { get; set; }
    [Required] public string AppId { get; set; }
}