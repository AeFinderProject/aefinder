using System.ComponentModel.DataAnnotations;

namespace AeFinder.Models;

public class CreateNewAppDto
{
    [Required]
    public string AppId { get; set; }
    [Required]
    public string Version { get; set; }
    public string ImageName { get; set; }
}