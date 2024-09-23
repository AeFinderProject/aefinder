using System.Text;
using Microsoft.Extensions.Options;

namespace AeFinder.RequestProxy;

public class RequestProxyService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string[] _urls;
    private readonly string[] _blockedPathPrefixes;
    private int _index;

    public RequestProxyService(IHttpClientFactory httpClientFactory, IOptionsMonitor<ProxyOptions> proxyOptions)
    {
        _httpClientFactory = httpClientFactory;
        _urls = proxyOptions.CurrentValue.Urls;
        _blockedPathPrefixes = proxyOptions.CurrentValue.BlockedPathPrefixes;
    }

    public async Task<string> ForwardSearchGetRequestAsync(string path, string method, string requestPayload)
    {
        if (path.Contains("/"))
        {
            throw new Exception("Invalid path");
        }

        if (_blockedPathPrefixes.Any(path.StartsWith))
        {
            throw new Exception("Blocked path");
        }

        if (method.Contains("/"))
        {
            throw new Exception("Invalid method");
        }

        var baseUrl = GetBaseUrl();

        var url = string.Format("{0}/{1}/{2}", baseUrl, path, method);
        
        var client = _httpClientFactory.CreateClient();
        
        var content = new StringContent(requestPayload, Encoding.UTF8, "application/json");
        
        var request = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Content = content
        };

        var response = await client.SendAsync(request);
        
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    private string GetBaseUrl()
    {
        var url = _urls[_index];
        _index += 1;
        _index %= _urls.Length;
        return url;
    }
}