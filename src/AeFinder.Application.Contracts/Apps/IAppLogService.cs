using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Apps.Dto;

namespace AeFinder.Apps;

public interface IAppLogService
{
    Task<List<AppLogRecordDto>> GetLatestRealTimeLogs(string nameSpace, string startTime, string appId,
        string version, List<string> levels = null, string id = null, string searchKeyWord = null,
        string chainId = null);
}