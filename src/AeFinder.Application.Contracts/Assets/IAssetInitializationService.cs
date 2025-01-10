using System;
using System.Threading.Tasks;

namespace AeFinder.Assets;

public interface IAssetInitializationService
{
    Task InitializeAsync(Guid organizationId);
}