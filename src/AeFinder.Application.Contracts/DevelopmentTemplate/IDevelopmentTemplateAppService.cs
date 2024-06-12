using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace AeFinder.DevelopmentTemplate;

public interface IDevelopmentTemplateAppService
{
    Task<FileContentResult> GenerateProjectAsync(GenerateProjectDto input);
}