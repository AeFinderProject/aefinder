using System.Collections.Generic;
using System.Threading.Tasks;

namespace AeFinder.AppResources;

public interface IAppResourceService
{
    Task<List<AppResourceDto>> GetAsync(string appId);
}