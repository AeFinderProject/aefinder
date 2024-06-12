using System.ComponentModel.DataAnnotations;

namespace AeFinder.Models;

public class CreateNewAppInput : AppVersionInput
{
    [Required]
    public string ImageName { get; set; }
}