using System;
using System.Threading.Tasks;

namespace AeFinder.ApiTraffic;

public interface IApiTrafficService
{
    Task IncreaseRequestCountAsync(string key);
    Task<long> GetRequestCountAsync(string key, DateTime dateTime);
}