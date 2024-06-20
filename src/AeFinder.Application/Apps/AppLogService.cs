using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Apps.Dto;
using AeFinder.Logger;
using AeFinder.Logger.Entities;
using AElf.EntityMapping.Repositories;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace AeFinder.Apps;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class AppLogService : AeFinderAppService, IAppLogService
{
    private readonly ILogService _logService;
    
    public AppLogService(ILogService logService)
    {
        _logService = logService;
    }

    public async Task<List<AppLogRecordDto>> GetLatestRealTimeLogs(string nameSpace, string startTime, string appId,
        string version, string level = null, string id = null, string searchKeyWord = null)
    {
        if (appId.IsNullOrEmpty())
        {
            throw new UserFriendlyException(
                $"Invalid appId: '{appId}'. Please provide a valid version.");
        }

        if (version.IsNullOrEmpty())
        {
            throw new UserFriendlyException(
                $"Invalid version: '{version}'. Please provide a valid version.");
        }

        if (startTime.IsNullOrEmpty())
        {
            var indexName = GetLogIndexName(nameSpace, appId, DateTime.Now);
            var result =
                await _logService.GetAppLatestLogAsync(indexName, AeFinderLoggerConsts.DefaultAppLogPageSize, 1,
                    version, level, searchKeyWord);
            return ObjectMapper.Map<List<AppLogIndex>, List<AppLogRecordDto>>(result);
        }

        DateTime startDateTime = DateTime.MaxValue;
        if (!DateTime.TryParse(startTime, out startDateTime) || startDateTime == DateTime.MaxValue)
        {
            throw new UserFriendlyException(
                $"Invalid start time format: '{startTime}'. Please provide a valid datetime.");
        }

        var logIndexName = GetLogIndexName(nameSpace, appId, startDateTime);

        var appLogs = await _logService.GetAppLogByStartTimeAsync(logIndexName,
            AeFinderLoggerConsts.DefaultAppLogPageSize, startTime, 1, version, level, id, searchKeyWord);
        appLogs = appLogs.OrderByDescending(x => x.App_log.Time).ThenByDescending(x => x.Log_id).ToList();
        return ObjectMapper.Map<List<AppLogIndex>, List<AppLogRecordDto>>(appLogs);

    }

    private string GetLogIndexName(string nameSpace,string appId,DateTime startDateTime)
    {
        return $"{nameSpace}-{appId}-log-index-{startDateTime.ToString("yyyy-MM")}".ToLower();
    }
}