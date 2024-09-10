using AeFinder.App.Es;
using AeFinder.User.Eto;
using AElf.EntityMapping.Repositories;
using Shouldly;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.Tests.Handlers;

public class OrganizationCreateHandlerTests : AeFinderBackgroundWorkerTestBase
{
    private readonly IDistributedEventHandler<OrganizationCreateEto> _distributedEventOrganizationCreate;
    private readonly IEntityMappingRepository<OrganizationIndex, string> _organizationEntityMappingRepository;
    
    public OrganizationCreateHandlerTests()
    {
        _distributedEventOrganizationCreate = GetRequiredService<IDistributedEventHandler<OrganizationCreateEto>>();
        _organizationEntityMappingRepository = GetRequiredService<IEntityMappingRepository<OrganizationIndex, string>>();
    }
    
    [Fact]
    public async Task CreateOrganization_Test()
    {
        var organizationEto = new OrganizationCreateEto()
        {
            OrganizationId = "abcd123",
            MaxAppCount = 3,
            OrganizationName = "AElfProject"
        };
        await _distributedEventOrganizationCreate.HandleEventAsync(organizationEto);
        var queryable = await _organizationEntityMappingRepository.GetQueryableAsync();
        queryable = queryable.Where(x => x.Id == "abcd123");
        var result = queryable.OrderBy(x => x.OrganizationName).ToList();
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].OrganizationName.ShouldBe("AElfProject");
    }
}