using System.ComponentModel.DataAnnotations;

namespace AeFinder.Models;

public class UpdateAppsDockerImageInput: AppIdsInput
{
    [Required]
    public string ImageName { get; set; }
    public bool IsUpdateConfig { get; set; }
}