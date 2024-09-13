using AeFinder.App.Es;
using AeFinder.BackgroundWorker.EventHandler;
using AeFinder.User.Eto;
using AElf.EntityMapping.Repositories;
using Shouldly;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.Tests.Handlers;

public class OrganizationCreateHandlerTests : AeFinderBackgroundWorkerCoreTestBase
{
    private readonly IDistributedEventHandler<OrganizationCreateEto> _distributedEventOrganizationCreate;
    private readonly IEntityMappingRepository<OrganizationIndex, string> _organizationEntityMappingRepository;
    
    public OrganizationCreateHandlerTests()
    {
        _distributedEventOrganizationCreate = GetRequiredService<OrganizationCreateHandler>();
        _organizationEntityMappingRepository = GetRequiredService<IEntityMappingRepository<OrganizationIndex, string>>();
    }
    
    [Fact]
    public async Task CreateOrganization_Test()
    {
        var organizationEto = new OrganizationCreateEto()
        {
            OrganizationId = "9c77dbf6-8222-d919-d38a-3a14710c091f",
            MaxAppCount = 3,
            OrganizationName = "AElfProject"
        };
        await _distributedEventOrganizationCreate.HandleEventAsync(organizationEto);
        var queryable = await _organizationEntityMappingRepository.GetQueryableAsync();
        queryable = queryable.Where(x => x.Id == "9c77dbf6-8222-d919-d38a-3a14710c091f");
        var result = queryable.OrderBy(x => x.OrganizationName).ToList();
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].OrganizationName.ShouldBe("AElfProject");
    }
}