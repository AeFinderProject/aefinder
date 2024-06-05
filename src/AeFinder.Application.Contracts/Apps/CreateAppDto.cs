using System.ComponentModel.DataAnnotations;

namespace AeFinder.Apps;

public class CreateAppDto
{
    public string AppId { get; set; }
    public string OrganizationId { get; set; }
    public string DeployKey { get; set; }
    [MinLength(2),MaxLength(20)]
    [RegularExpression("[A-Za-z0-9\\s]+" ,ErrorMessage = "The AppName can only contain letters('A'-'Z', 'a'-'z'), numbers(0-9), and spaces(' ').")]
    public string AppName { get; set; }
    [MaxLength(200)]
    public string ImageUrl { get; set; }
    [MaxLength(500)]
    public string Description { get; set; }
    [MaxLength(200)]
    public string SourceCodeUrl { get; set; }
}