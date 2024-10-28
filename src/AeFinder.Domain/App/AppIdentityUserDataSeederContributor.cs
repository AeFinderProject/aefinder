using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Uow;

namespace AeFinder.App;

public class AppIdentityUserDataSeederContributor: IDataSeedContributor, ITransientDependency
{
    public AppIdentityUserDataSeederContributor()
    {
    }
    
    [UnitOfWork]
    public async Task SeedAsync(DataSeedContext context)
    {
        await SeedAppIdentityUsersAsync();
    }

    private async Task SeedAppIdentityUsersAsync()
    {
        
    }
}