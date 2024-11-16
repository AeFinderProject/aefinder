using System;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AeFinder.ApiTraffic;

public class ApiTrafficServiceTests : AeFinderApplicationAppTestBase
{
    private readonly IApiTrafficService _apiTrafficService;
    private readonly IApiTrafficProvider _apiTrafficProvider;

    public ApiTrafficServiceTests()
    {
        _apiTrafficService = GetRequiredService<IApiTrafficService>();
        _apiTrafficProvider = GetRequiredService<IApiTrafficProvider>();
    }

    [Fact]
    public async Task RequestCountTest()
    {
        var apiKey1 = "APIKEY1";
        var apiKey2 = "APIKEY2";

        await _apiTrafficService.IncreaseRequestCountAsync(apiKey1);
        await _apiTrafficService.IncreaseRequestCountAsync(apiKey1);
        await _apiTrafficService.IncreaseRequestCountAsync(apiKey2);

        var apiKeyCount1 = await _apiTrafficService.GetRequestCountAsync(apiKey1, DateTime.UtcNow);
        apiKeyCount1.ShouldBe(0);
        var apiKeyCount2 = await _apiTrafficService.GetRequestCountAsync(apiKey2, DateTime.UtcNow);
        apiKeyCount2.ShouldBe(0);

        await _apiTrafficProvider.FlushAsync();
        
        apiKeyCount1 = await _apiTrafficService.GetRequestCountAsync(apiKey1, DateTime.UtcNow);
        apiKeyCount1.ShouldBe(2);
        apiKeyCount2 = await _apiTrafficService.GetRequestCountAsync(apiKey2, DateTime.UtcNow);
        apiKeyCount2.ShouldBe(1);
        
        await _apiTrafficService.IncreaseRequestCountAsync(apiKey1);
        await _apiTrafficService.IncreaseRequestCountAsync(apiKey1);
        await _apiTrafficService.IncreaseRequestCountAsync(apiKey2);
        
        apiKeyCount1 = await _apiTrafficService.GetRequestCountAsync(apiKey1, DateTime.UtcNow);
        apiKeyCount1.ShouldBe(2);
        apiKeyCount2 = await _apiTrafficService.GetRequestCountAsync(apiKey2, DateTime.UtcNow);
        apiKeyCount2.ShouldBe(1);
        
        await _apiTrafficProvider.FlushAsync();
        
        apiKeyCount1 = await _apiTrafficService.GetRequestCountAsync(apiKey1, DateTime.UtcNow);
        apiKeyCount1.ShouldBe(4);
        apiKeyCount2 = await _apiTrafficService.GetRequestCountAsync(apiKey2, DateTime.UtcNow);
        apiKeyCount2.ShouldBe(2);
    }
}