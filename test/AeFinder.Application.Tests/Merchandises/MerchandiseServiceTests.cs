using System.Threading.Tasks;
using Orleans;
using Shouldly;
using Volo.Abp.Timing;
using Xunit;

namespace AeFinder.Merchandises;

public class MerchandiseServiceTests : AeFinderApplicationTestBase
{
    private readonly IClusterClient _clusterClient;
    private readonly IClock _clock;
    private readonly IMerchandiseService _merchandiseService;

    public MerchandiseServiceTests()
    {
        _clusterClient = GetRequiredService<IClusterClient>();
        _clock = GetRequiredService<IClock>();
        _merchandiseService = GetRequiredService<IMerchandiseService>();
    }

    [Fact]
    public async Task GetListTest()
    {
        var list = await _merchandiseService.GetListAsync(new GetMerchandiseInput());
        list.Items.Count.ShouldBe(5);
        
        list = await _merchandiseService.GetListAsync(new GetMerchandiseInput
        {
            Category = MerchandiseCategory.Resource
        });
        list.Items.Count.ShouldBe(4);
        
        list = await _merchandiseService.GetListAsync(new GetMerchandiseInput
        {
            Type = MerchandiseType.Processor
        });
        list.Items.Count.ShouldBe(3);
    }
}