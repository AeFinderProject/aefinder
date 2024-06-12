using System.Threading.Tasks;
using AeFinder.Apps.Dto;

namespace AeFinder.Apps;

public interface IAppLogService
{
    Task<AppLogRecordDto> GetLatestRealTimeLogs(string startTime, string appId, string version);
}