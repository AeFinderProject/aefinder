using System.ComponentModel.DataAnnotations;

namespace AeFinder.AppResources.Dto;

public class GetAppFullPodResourceInfoInput
{
    [Required]
    public string AppId { get; set; }
    public string Version { get; set; }
}