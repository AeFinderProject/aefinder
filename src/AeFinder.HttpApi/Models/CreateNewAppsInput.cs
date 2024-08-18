using System.ComponentModel.DataAnnotations;

namespace AeFinder.Models;

public class CreateNewAppsInput : AppIdsInput
{
    [Required]
    public string ImageName { get; set; }
}