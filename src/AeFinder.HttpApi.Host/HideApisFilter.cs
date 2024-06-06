using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AeFinder;

public class HideApisFilter: IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Hide all apis that contain "/api/account"
        var pathsToRemoveAccountAPI = swaggerDoc.Paths
            .Where(path => path.Key.StartsWith("/api/account"))
            .ToList();

        foreach (var path in pathsToRemoveAccountAPI)
        {
            swaggerDoc.Paths.Remove(path.Key);
        }
    }
    
}