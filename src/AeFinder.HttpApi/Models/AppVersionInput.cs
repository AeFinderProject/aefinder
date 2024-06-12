using System.ComponentModel.DataAnnotations;

namespace AeFinder.Models;

public class AppVersionInput : AppInput
{
    [Required] public string Version { get; set; }
}