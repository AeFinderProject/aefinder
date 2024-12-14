using System.Text;
using AeFinder.Logger.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;

namespace AeFinder.Logger;

public class LogElasticSearchService:ILogService
{
    private readonly ElasticClient _elasticClient;
    private readonly ILogger<LogElasticSearchService> _logger;
    private readonly LogElasticSearchOptions _logElasticSearchOptions;

    public LogElasticSearchService(ILogger<LogElasticSearchService> logger, ElasticClient elasticClient,
        IOptionsSnapshot<LogElasticSearchOptions> logElasticSearchOptions)
    {
        _logger = logger;
        _elasticClient = elasticClient;
        _logElasticSearchOptions = logElasticSearchOptions.Value;
    }

    public async Task<List<AppLogIndex>> GetAppLatestLogAsync(string indexName, int pageSize,
        int eventId, string appVersion, List<string> levels, string searchKeyWord, string chainId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<AppLogIndex>, QueryContainer>>();
        mustQuery.Add(m => m.Term(t => t.Field(f => f.App_log.EventId).Value(eventId)));
        mustQuery.Add(m => m.Term(t => t.Field(f => f.App_log.Version).Value(appVersion)));
        if (!levels.IsNullOrEmpty())
        {
            // mustQuery.Add(q => q.Term(i => i.Field(f => f.App_log.Level).Value(level)));
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.App_log.Level).Terms(levels)));
        }
        
        if (!chainId.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.App_log.ChainId).Value(chainId)));
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
        int eventId, string appVersion, List<string> levels, string logId, string searchKeyWord, string chainId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<AppLogIndex>, QueryContainer>>();
        mustQuery.Add(m => m.Term(t => t.Field(f => f.App_log.EventId).Value(eventId)));
        mustQuery.Add(m => m.Term(t => t.Field(f => f.App_log.Version).Value(appVersion)));
        if (!levels.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.App_log.Level).Terms(levels)));
        }
        
        if (!chainId.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.App_log.ChainId).Value(chainId)));
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

    public string GetAppLogIndexAliasName(string nameSpace, string appId, string version)
    {
        return $"{nameSpace}-{appId}-{version}-log-index".ToLower();
    }

    public async Task CreateFileBeatLogILMPolicyAsync(string policyName)
    {
        // var getPolicyResponse = await _elasticClient.IndexLifecycleManagement.GetLifecycleAsync(g => g.PolicyId(policyName));
        //
        // if (getPolicyResponse.IsValid && getPolicyResponse.Policies.ContainsKey(policyName))
        // {
        //     _logger.LogInformation($"FileBeat log ILM policy {policyName} already exists.");
        //     return;
        // }

        if (_logElasticSearchOptions == null || _logElasticSearchOptions.Uris == null)
        {
            return;
        }

        var putPolicyResponse = await _elasticClient.IndexLifecycleManagement.PutLifecycleAsync(policyName, p => p
            .Policy(pd => pd
                .Phases(ph => ph
                    .Hot(h => h
                        .MinimumAge("0ms")
                        .Actions(a => a
                            .Rollover(ro => ro
                                .MaximumSize(_logElasticSearchOptions.ILMPolicy.HotMaxSize)
                                .MaximumAge(_logElasticSearchOptions.ILMPolicy.HotMaxAge))
                            .SetPriority(pp => pp.Priority(100))
                        )
                    )
                    .Cold(c => c
                        .MinimumAge(_logElasticSearchOptions.ILMPolicy.ColdMinAge)
                        .Actions(a => a
                            .Freeze(f => f)
                            .SetPriority(pp => pp.Priority(50))
                        )
                    )
                    .Delete(d => d
                        .MinimumAge(_logElasticSearchOptions.ILMPolicy.DeleteMinAge)
                        .Actions(a => a
                            .Delete(de => de)
                        )
                    )
                )
            )
        );

        if (putPolicyResponse.IsValid)
        {
            _logger.LogInformation("ILM policy is created successfully. ");
        }
        else
        {
            _logger.LogError($"Failed to create an ILM policy: {putPolicyResponse.DebugInformation}");
        }
    }
}