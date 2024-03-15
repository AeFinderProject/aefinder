using System.ComponentModel.DataAnnotations;

namespace AeFinder.Studio;

public class AddOrUpdateAeFinderAppInput : AeFinderAppInfo
{
    [Required] public string AppId { get; set; }
}