using System.ComponentModel.DataAnnotations;
using Orleans;

namespace AeFinder.Apps;

[GenerateSerializer]
public class CreateAppDto
{
    [Id(0)] public string AppId { get; set; }
    [Id(1)] public string OrganizationId { get; set; }
    [Id(2)] public string DeployKey { get; set; }
    [MinLength(2),MaxLength(20)]
    [RegularExpression("[A-Za-z0-9\\s]+" ,ErrorMessage = "The AppName can only contain letters('A'-'Z', 'a'-'z'), numbers(0-9), and spaces(' ').")]
    [Id(3)] public string AppName { get; set; }
    [MaxLength(200)]
    [Id(4)] public string ImageUrl { get; set; }
    [MaxLength(500)]
    [Id(5)] public string Description { get; set; }
    [MaxLength(200)]
    [Id(6)] public string SourceCodeUrl { get; set; }
}