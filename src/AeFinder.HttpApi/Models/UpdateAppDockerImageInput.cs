using System.ComponentModel.DataAnnotations;

namespace AeFinder.Models;

public class UpdateAppDockerImageInput: AppVersionInput
{
    [Required]
    public string ImageName { get; set; }
    public bool IsUpdateConfig { get; set; }
}