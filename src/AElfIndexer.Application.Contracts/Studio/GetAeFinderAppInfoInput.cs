using System.ComponentModel.DataAnnotations;

namespace AElfIndexer.Studio;

public class GetAeFinderAppInfoInput
{
    [Required] public string Name { get; set; }
}