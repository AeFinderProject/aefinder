using System.ComponentModel.DataAnnotations;
using Orleans;

namespace AeFinder.Apps;

[GenerateSerializer]
public class UpdateAppDto
{
    [MaxLength(200)]
    [Id(0)] public string ImageUrl { get; set; }
    [MaxLength(500)]
    [Id(1)] public string Description { get; set; }
    [MaxLength(200)]
    [Id(2)] public string SourceCodeUrl { get; set; }
}