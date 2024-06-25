using AeFinder.Logger.Entities;

namespace AeFinder.Logger;

public interface ILogService
{
    Task<List<AppLogIndex>> GetAppLatestLogAsync(string indexName, int pageSize,
        int eventId, string appVersion, string level, string searchKeyWord);

    Task<List<AppLogIndex>> GetAppLogByStartTimeAsync(string indexName, int pageSize, string startTime,
        int eventId, string appVersion, string level, string logId, string searchKeyWord);

    Task SetAppLogAliasAsync(string nameSpace, string appId, string version);

    string GetAppLogIndexAliasName(string nameSpace, string appId, string version);
}