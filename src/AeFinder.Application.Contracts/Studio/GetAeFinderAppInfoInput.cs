using System.ComponentModel.DataAnnotations;

namespace AeFinder.Studio;

public class GetAeFinderAppInfoInput
{
    [Required] public string AppId { get; set; }
}