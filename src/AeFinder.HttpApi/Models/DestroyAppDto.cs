using System.ComponentModel.DataAnnotations;

namespace AeFinder.Models;

public class DestroyAppDto
{
    [Required]
    public string AppId { get; set; }
    [Required]
    public string Version { get; set; }
}