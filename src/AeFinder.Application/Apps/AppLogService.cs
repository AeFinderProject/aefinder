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
        string version, List<string> levels = null, string id = null, string searchKeyWord = null,
        string chainId = null)
    {
        if (appId.IsNullOrEmpty())
        {
            throw new UserFriendlyException(
                $"Invalid appId: '{appId}'. Please provide a valid appId.");
        }

        if (version.IsNullOrEmpty())
        {
            throw new UserFriendlyException(
                $"Invalid version: '{version}'. Please provide a valid version.");
        }

        var logIndexName = _logService.GetAppLogIndexAliasName(nameSpace, appId, version);

        if (startTime.IsNullOrEmpty())
        {
            var result =
                await _logService.GetAppLatestLogAsync(logIndexName, AeFinderLoggerConsts.DefaultAppLogPageSize, 1,
                    version, levels, searchKeyWord, chainId);
            return ObjectMapper.Map<List<AppLogIndex>, List<AppLogRecordDto>>(result);
        }

        DateTime startDateTime = DateTime.MaxValue;
        if (!DateTime.TryParse(startTime, out startDateTime) || startDateTime == DateTime.MaxValue)
        {
            throw new UserFriendlyException(
                $"Invalid start time format: '{startTime}'. Please provide a valid datetime.");
        }

        var appLogs = await _logService.GetAppLogByStartTimeAsync(logIndexName,
            AeFinderLoggerConsts.DefaultAppLogPageSize, startTime, 1, version, levels, id, searchKeyWord, chainId);
        appLogs = appLogs.OrderByDescending(x => x.App_log.Time).ThenByDescending(x => x.Log_id).ToList();
        return ObjectMapper.Map<List<AppLogIndex>, List<AppLogRecordDto>>(appLogs);

    }

}