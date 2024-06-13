using AeFinder.Logger.Entities;
using Nest;

namespace AeFinder.Logger;

public class LogElasticSearchService:ILogService
{
    private readonly ElasticClient _elasticClient;

    public LogElasticSearchService(ElasticClient elasticClient)
    {
        _elasticClient = elasticClient;
    }

    public async Task<List<AppLogIndex>> GetAppLogByStartTimeAsync(string indexName, int pageSize, string startTime,
        int eventId, string appVersion)
    {
        var searchAfter = new List<string>();
        searchAfter.Add(startTime);

        var response = await _elasticClient.SearchAsync<AppLogIndex>(s => s
            .Index(indexName)
            .Sort(so => so
                .Ascending(f => f.App_log.Time))
            .SearchAfter(searchAfter.Cast<object>().ToArray())
            .Size(pageSize)
            .Query(q => q
                .Bool(b => b
                    .Must(
                        m => m.Term(t => t.Field(f => f.App_log.EventId).Value(eventId)),
                        m => m.Term(t => t.Field(f => f.App_log.Version).Value(appVersion))
                    ))));

        return response.Documents.ToList();
    }

    public async Task<List<AppLogIndex>> GetAppLogByStartTimeAsync(string indexName, int pageSize, string startTime,
        int eventId, string appVersion, string logId)
    {
        var searchAfter = new List<string>();
        searchAfter.Add(startTime);
        searchAfter.Add(logId);
        var response = await _elasticClient.SearchAsync<AppLogIndex>(s => s
            .Index(indexName)
            .Sort(so => so
                .Ascending(f => f.App_log.Time)
                .Ascending(f => f.Log_id))
            .SearchAfter(searchAfter.Cast<object>().ToArray())
            .Size(pageSize)
            .Query(q => q
                .Bool(b => b
                    .Must(
                        m => m.Term(t => t.Field(f => f.App_log.EventId).Value(eventId)),
                        m => m.Term(t => t.Field(f => f.App_log.Version).Value(appVersion))
                    ))));

        return response.Documents.ToList();
    }
}