using System;
using System.Threading.Tasks;
using AeFinder.App.ES;
using AeFinder.Apps.Dto;
using AElf.EntityMapping.Repositories;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace AeFinder.Apps;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class AppLogService : AeFinderAppService, IAppLogService
{
    private readonly IEntityMappingRepository<AppLogIndex, string> _appLogIndexRepository;
    
    public AppLogService(IEntityMappingRepository<AppLogIndex, string> appLogIndexRepository)
    {
        _appLogIndexRepository = appLogIndexRepository;
    }

    public async Task<AppLogRecordDto> GetLatestRealTimeLogs(string startTime, string appId, string version)
    {
        DateTime startDateTime = DateTime.MaxValue;
        if (!DateTime.TryParse(startTime, out startDateTime) || startDateTime == DateTime.MaxValue)
        {
            throw new UserFriendlyException(
                $"Invalid start time format: '{startTime}'. Please provide a valid datetime.");
        }

        return new AppLogRecordDto()
        {
            @Timestamp = DateTime.Now,
            Environment = "TestNet",
            App_log = new AppLogInfo()
            {
                AppId = "aelfscan-genesis",
                Version = "ad8752cac1674b86b8dd27c331226c5e",
                EventId = 1,
                Exception = "",
                Level = "Debug",
                Message = "",
                Time = DateTime.Now
            }
        };

    }
}