using System.ComponentModel.DataAnnotations;

namespace AeFinder.Models;

public class RestartAppDto
{
    [Required]
    public string AppId { get; set; }
    [Required]
    public string Version { get; set; }
}