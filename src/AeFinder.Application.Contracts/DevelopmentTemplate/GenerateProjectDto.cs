using System.ComponentModel.DataAnnotations;

namespace AeFinder.DevelopmentTemplate;

public class GenerateProjectDto
{
    [MinLength(2),MaxLength(20)]
    [RegularExpression("[A-Za-z][A-Za-z0-9.]+" ,ErrorMessage = "The Name must begin with a letter and can only contain letters('A'-'Z', 'a'-'z'), numbers(0-9), and dots('.').")]
    public string Name { get; set; }
}