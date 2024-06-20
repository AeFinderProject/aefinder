using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Apps.Dto;

namespace AeFinder.Apps;

public interface IAppLogService
{
    Task<List<AppLogRecordDto>> GetLatestRealTimeLogs(string nameSpace, string startTime, string appId,
        string version, string level = null, string id = null, string searchKeyWord = null);
}