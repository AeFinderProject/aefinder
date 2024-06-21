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

    public async Task<List<AppLogIndex>> GetAppLatestLogAsync(string indexName, int pageSize,
        int eventId, string appVersion, string level, string searchKeyWord)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<AppLogIndex>, QueryContainer>>();
        mustQuery.Add(m => m.Term(t => t.Field(f => f.App_log.EventId).Value(eventId)));
        mustQuery.Add(m => m.Term(t => t.Field(f => f.App_log.Version).Value(appVersion)));
        if (!level.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.App_log.Level).Value(level)));
        }

        var shouldQuery = new List<Func<QueryContainerDescriptor<AppLogIndex>, QueryContainer>>();
        if (!string.IsNullOrEmpty(searchKeyWord))
        {
            shouldQuery.Add(q => q.Wildcard(w => w
                .Field(f => f.App_log.Message)
                .Value($"*{searchKeyWord}*")));

            shouldQuery.Add(q => q.Wildcard(w => w
                .Field(f => f.App_log.Exception)
                .Value($"*{searchKeyWord}*")));
        }
        QueryContainer Filter(QueryContainerDescriptor<AppLogIndex> f) 
        {
            var boolQuery = new BoolQueryDescriptor<AppLogIndex>()
                .Must(mustQuery);

            if (shouldQuery.Any())
            {
                boolQuery.Should(shouldQuery)
                    .MinimumShouldMatch(1);
            }

            return f.Bool(b => boolQuery);
        }
        
        var response = await _elasticClient.SearchAsync<AppLogIndex>(s => s
            .Index(indexName)
            .Sort(so => so
                .Descending(f => f.App_log.Time))
            .Size(pageSize)
            .Query(Filter));

        return response.Documents.ToList();
    }

    public async Task<List<AppLogIndex>> GetAppLogByStartTimeAsync(string indexName, int pageSize, string startTime,
        int eventId, string appVersion, string level, string logId, string searchKeyWord)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<AppLogIndex>, QueryContainer>>();
        mustQuery.Add(m => m.Term(t => t.Field(f => f.App_log.EventId).Value(eventId)));
        mustQuery.Add(m => m.Term(t => t.Field(f => f.App_log.Version).Value(appVersion)));
        if (!level.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.App_log.Level).Value(level)));
        }
        
        Func<SortDescriptor<AppLogIndex>, IPromise<IList<ISort>>> sort = null;
        var searchAfter = new List<string>();
        searchAfter.Add(startTime);
        if (logId.IsNullOrEmpty())
        {
            sort = s =>
                s.Ascending(a => a.App_log.Time);
        }
        else
        {
            searchAfter.Add(logId);
            sort = s =>
                s.Ascending(a => a.App_log.Time).Ascending(d => d.Log_id);
        }

        var shouldQuery = new List<Func<QueryContainerDescriptor<AppLogIndex>, QueryContainer>>();
        if (!string.IsNullOrEmpty(searchKeyWord))
        {
            shouldQuery.Add(q => q.Wildcard(w => w
                .Field(f => f.App_log.Message)
                .Value($"*{searchKeyWord}*")));

            shouldQuery.Add(q => q.Wildcard(w => w
                .Field(f => f.App_log.Exception)
                .Value($"*{searchKeyWord}*")));
        }
        QueryContainer Filter(QueryContainerDescriptor<AppLogIndex> f) 
        {
            var boolQuery = new BoolQueryDescriptor<AppLogIndex>()
                .Must(mustQuery);

            if (shouldQuery.Any())
            {
                boolQuery.Should(shouldQuery)
                    .MinimumShouldMatch(1);
            }

            return f.Bool(b => boolQuery);
        }

        var response = await _elasticClient.SearchAsync<AppLogIndex>(s => s
            .Index(indexName)
            .Sort(sort)
            .SearchAfter(searchAfter.Cast<object>().ToArray())
            .Size(pageSize)
            .Query(Filter));

        return response.Documents.ToList();
    }
}