using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Logger;
using AeFinder.Logger.Entities;
using Elasticsearch.Net;
using Nest;
using Shouldly;
using Xunit;

namespace AeFinder.Apps;

public class AppLogServiceTest: AeFinderApplicationAppTestBase
{
    private readonly IAppLogService _appLogService;
    private readonly ElasticClient _elasticClient;
    private readonly ILogService _logService;

    public AppLogServiceTest()
    {
        _appLogService = GetRequiredService<IAppLogService>();
        _elasticClient = GetRequiredService<ElasticClient>();
        _logService=GetRequiredService<ILogService>();
    }

    [Fact]
    public async Task GetLatestRealTimeLogsTest()
    {
        var logList = new List<AppLogIndex>
        {
            new AppLogIndex(){Log_id = "IpuvL5MBjm-icn01p_Hw",Timestamp = DateTime.Now,Environment = "indexerUnitTest",App_log = new AppLogDetail()
            {
                ChainId="AELF",
                Exception = "",
                AppId = "testIndexer",
                Version = "c071942502d54d369319b145355b8c82",
                EventId = 1,
                Level = "Debug",
                Time = DateTime.Now,
                Message = "test log message"
            }},
            new AppLogIndex(){Log_id = "IJuvL5MBjm-icn01p_Hw",Timestamp = DateTime.Now.AddDays(-1),Environment = "indexerUnitTest",App_log = new AppLogDetail()
            {
                ChainId="AELF",
                Exception = "",
                AppId = "testIndexer",
                Version = "c071942502d54d369319b145355b8c82",
                EventId = 1,
                Level = "Error",
                Time = DateTime.Now.AddDays(-1),
                Message = "log message"
            }},
            new AppLogIndex(){Log_id = "IJuvL5MBjm-icn01p_Hw",Timestamp = DateTime.Now,Environment = "indexerUnitTest",App_log = new AppLogDetail()
            {
                ChainId="tDVV",
                Exception = "",
                AppId = "testIndexer",
                Version = "c071942502d54d369319b145355b8c82",
                EventId = 1,
                Level = "Debug",
                Time = DateTime.Now,
                Message = "log message test"
            }}
        };
        var nameSpace="aefinder-app";
        var indexName = _logService.GetAppLogIndexAliasName(nameSpace,"testIndexer","c071942502d54d369319b145355b8c82");
        _elasticClient.Indices.Create(indexName, c => c
            .Map<AppLogIndex>(m => m.AutoMap())
        );
        
        foreach (var appLog in logList)
        {
            var response = _elasticClient.Index(appLog, idx => idx.Index(indexName).Refresh(Refresh.True));;
            if (response.IsValid)
            {
                Console.WriteLine($"Document indexed successfully into {indexName}.");
            }
        }

        var logs = await _appLogService.GetLatestRealTimeLogs(nameSpace, null,
            "testIndexer", "c071942502d54d369319b145355b8c82");
        logs.Count.ShouldBe(3);
        
        var searchKeywordLogs=await _appLogService.GetLatestRealTimeLogs(nameSpace, null,
            "testIndexer", "c071942502d54d369319b145355b8c82",searchKeyWord:"test");
        searchKeywordLogs.Count.ShouldBe(2);
        
        var chainFilterLogs=await _appLogService.GetLatestRealTimeLogs(nameSpace, null,
            "testIndexer", "c071942502d54d369319b145355b8c82",chainId:"AELF");
        chainFilterLogs.Count.ShouldBe(2);
        
        var leverFilterLogs=await _appLogService.GetLatestRealTimeLogs(nameSpace, null,
            "testIndexer", "c071942502d54d369319b145355b8c82",levels:new List<string>(){"Error"});
        leverFilterLogs.Count.ShouldBe(1);
        
        var startTimeFilterLogs = await _appLogService.GetLatestRealTimeLogs(nameSpace, leverFilterLogs[0].App_log.Time.ToString("o"),
            "testIndexer", "c071942502d54d369319b145355b8c82",id:leverFilterLogs[0].Log_id);
        startTimeFilterLogs.Count.ShouldBe(2);
    }
}