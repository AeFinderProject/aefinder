using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.App.Es;
using AeFinder.User;
using AeFinder.User.Dto;
using AElf.EntityMapping.Repositories;
using Shouldly;
using Xunit;

namespace AeFinder.Apps;

public class OrganizationAppServiceTests: AeFinderApplicationAppTestBase
{
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IEntityMappingRepository<OrganizationIndex, string> _organizationEntityMappingRepository;
    
    public OrganizationAppServiceTests()
    {
        _organizationAppService = GetRequiredService<OrganizationAppService>();
        _organizationEntityMappingRepository = GetRequiredService<IEntityMappingRepository<OrganizationIndex, string>>();
    }

    [Fact]
    public async Task OrganizationIndexList_Test()
    {
        for (int i = 0; i < 6; i++)
        {
            await _organizationEntityMappingRepository.AddAsync(new OrganizationIndex()
            {
                OrganizationId = Guid.NewGuid().ToString(),
                OrganizationName = "OrganizationName",
                MaxAppCount = i,
                AppIds = new List<string>(){"AppId" + i}
            });
        }

        var input = new GetOrganizationListInput()
        {
            SkipCount = 0,
            MaxResultCount = 5
        };
        var organizations = await _organizationAppService.GetOrganizationListAsync(input);
        organizations.Items.Count.ShouldBe(5);
        organizations.TotalCount.ShouldBe(6);
    }
}