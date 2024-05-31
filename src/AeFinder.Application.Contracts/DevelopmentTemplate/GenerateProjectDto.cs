using System.ComponentModel.DataAnnotations;

namespace AeFinder.DevelopmentTemplate;

public class GenerateProjectDto
{
    [MinLength(2),MaxLength(20)]
    [RegularExpression("[A-Za-z][A-Za-z0-9.]+")]
    public string Name { get; set; }
}