using System.ComponentModel.DataAnnotations;

namespace AeFinder.Models;

public class AppInput
{
    [Required]
    public string AppId { get; set; }
}