using AeFinder.Logger.Entities;

namespace AeFinder.Logger;

public interface ILogService
{
    Task<List<AppLogIndex>> GetAppLatestLogAsync(string indexName, int pageSize,
        int eventId, string appVersion, List<string> levels, string searchKeyWord, string chainId);

    Task<List<AppLogIndex>> GetAppLogByStartTimeAsync(string indexName, int pageSize, string startTime,
        int eventId, string appVersion, List<string> levels, string logId, string searchKeyWord, string chainId);

    string GetAppLogIndexAliasName(string nameSpace, string appId, string version);

    Task CreateFileBeatLogILMPolicyAsync(string policyName);
}