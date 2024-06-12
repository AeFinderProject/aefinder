using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Apps.Dto;
using AeFinder.Log;
using AeFinder.Log.Entities;
using AElf.EntityMapping.Repositories;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace AeFinder.Apps;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class AppLogService : AeFinderAppService, IAppLogService
{
    // private readonly IEntityMappingRepository<AppLogIndex, string> _appLogIndexRepository;
    private readonly ILogService _logService;
    
    public AppLogService(ILogService logService)
    {
        // _appLogIndexRepository = appLogIndexRepository;
        _logService = logService;
    }

    public async Task<List<AppLogRecordDto>> GetLatestRealTimeLogs(string nameSpace, string startTime, string appId,
        string version, string id = null)
    {
        DateTime startDateTime = DateTime.MaxValue;
        if (!DateTime.TryParse(startTime, out startDateTime) || startDateTime == DateTime.MaxValue)
        {
            throw new UserFriendlyException(
                $"Invalid start time format: '{startTime}'. Please provide a valid datetime.");
        }

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
        
        

        var logIndexName = GetLogIndexName(nameSpace, appId);
        
        if (id.IsNullOrEmpty())
        {
            var result = await _logService.GetAppLogByStartTimeAsync(logIndexName, 1000, startTime, 1, version);
            return ObjectMapper.Map<List<AppLogIndex>, List<AppLogRecordDto>>(result);
        }
        
        var appLogs = await _logService.GetAppLogByStartTimeAsync(logIndexName, 1000, startTime, 1, version,id);
        return ObjectMapper.Map<List<AppLogIndex>, List<AppLogRecordDto>>(appLogs);

        // return new List<AppLogRecordDto>()
        // {
        //     new AppLogRecordDto()
        //     {
        //         Id = "tvIwDJABk1kMdT6vWTzq",
        //         @Timestamp = DateTime.Now,
        //         Environment = "TestNet",
        //         App_log = new AppLogInfo()
        //         {
        //             AppId = "aelfscan-genesis",
        //             Version = "ad8752cac1674b86b8dd27c331226c5e",
        //             EventId = 1,
        //             Exception = "",
        //             Level = "Debug",
        //             Message = "",
        //             Time = DateTime.Now
        //         }
        //     }
        // };

    }

    private string GetLogIndexName(string nameSpace,string appId)
    {
        return $"{nameSpace}-{appId}-log-index".ToLower();
    }
}