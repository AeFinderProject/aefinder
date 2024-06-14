using AeFinder.Logger.Entities;

namespace AeFinder.Logger;

public interface ILogService
{
    Task<List<AppLogIndex>> GetAppLatestLogAsync(string indexName, int pageSize,
        int eventId, string appVersion);
    Task<List<AppLogIndex>> GetAppLogByStartTimeAsync(string indexName, int pageSize, string startTime,
        int eventId, string appVersion);

    Task<List<AppLogIndex>> GetAppLogByStartTimeAsync(string indexName, int pageSize, string startTime,
        int eventId, string appVersion, string logId);
}