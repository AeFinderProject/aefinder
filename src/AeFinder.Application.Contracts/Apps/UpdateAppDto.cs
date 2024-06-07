using System.ComponentModel.DataAnnotations;

namespace AeFinder.Apps;

public class UpdateAppDto
{
    [MaxLength(200)]
    public string ImageUrl { get; set; }
    [MaxLength(500)]
    public string Description { get; set; }
    [MaxLength(200)]
    public string SourceCodeUrl { get; set; }
}