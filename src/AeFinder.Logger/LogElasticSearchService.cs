using AeFinder.Logger.Entities;
using Microsoft.Extensions.Logging;
using Nest;

namespace AeFinder.Logger;

public class LogElasticSearchService:ILogService
{
    private readonly ElasticClient _elasticClient;
    private readonly ILogger<LogElasticSearchService> _logger;

    public LogElasticSearchService(ILogger<LogElasticSearchService> logger,ElasticClient elasticClient)
    {
        _logger = logger;
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

    public async Task SetAppLogAliasAsync(string nameSpace, string appId, string version)
    {
        string aliasName = GetAppLogIndexAliasName(nameSpace, appId, version);
        string indexPattern = aliasName + "-*";

        //Create an empty app log index in case 404 error occurs when creating an index alias
        var emptyLogIndexName = aliasName + "-empty";
        await CreateEmptyAppLogIndexAsync(emptyLogIndexName);
        //Create index alias
        var response = await _elasticClient.Indices.BulkAliasAsync(a => a
            .Add(add => add
                .Index(indexPattern)
                .Alias(aliasName)
            )
        );

        if (!response.IsValid)
        {
            _logger.LogError("Error adding alias: " + response.DebugInformation);
        }
        else
        {
            _logger.LogInformation($"Alias {aliasName} added successfully");
        }
    }

    private async Task CreateEmptyAppLogIndexAsync(string indexName)
    {
        var createIndexResponse = await _elasticClient.Indices.CreateAsync(indexName, c => c
            .Settings(s => s
                    .NumberOfShards(1)
                    .NumberOfReplicas(1)
            )
        );

        if (createIndexResponse.IsValid)
        {
            _logger.LogInformation($"Empty index {indexName} is created successfully.");
        }
        else
        {
            _logger.LogError($"Failed to create an empty index {indexName}：{createIndexResponse.OriginalException.Message}");
        }
    }

    public string GetAppLogIndexAliasName(string nameSpace, string appId, string version)
    {
        return $"{nameSpace}-{appId}-{version}-log-index".ToLower();
    }
}