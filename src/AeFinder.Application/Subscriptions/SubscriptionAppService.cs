using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.App.Deploy;
using AeFinder.App.Es;
using AeFinder.Apps.Eto;
using AeFinder.BlockScan;
using AeFinder.CodeOps;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.Subscriptions;
using AElf.EntityMapping.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.Subscriptions;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class SubscriptionAppService : AeFinderAppService, ISubscriptionAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly ICodeAuditor _codeAuditor;
    private readonly IAppDeployManager _appDeployManager;
    private readonly AppDeployOptions _appDeployOptions;
    private readonly IEntityMappingRepository<AppSubscriptionIndex, string> _subscriptionIndexRepository;

    public SubscriptionAppService(IClusterClient clusterClient, ICodeAuditor codeAuditor,
        IAppDeployManager appDeployManager, IOptionsSnapshot<AppDeployOptions> appDeployOptions,
        IEntityMappingRepository<AppSubscriptionIndex, string> subscriptionIndexRepository)
    {
        _clusterClient = clusterClient;
        _codeAuditor = codeAuditor;
        _appDeployManager = appDeployManager;
        _subscriptionIndexRepository = subscriptionIndexRepository;
        _appDeployOptions = appDeployOptions.Value;
    }

    public async Task<string> AddSubscriptionAsync(string appId, SubscriptionManifestDto manifest, byte[] code)
    {
        await CheckAppExistAsync(appId);
        CheckCode(code);
        
        var subscription = ObjectMapper.Map<SubscriptionManifestDto, SubscriptionManifest>(manifest);
        var appSubscriptionGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));

        var addResult = await appSubscriptionGrain.AddSubscriptionAsync(subscription, code);
        
        var rulePath = await _appDeployManager.CreateNewAppAsync(appId, addResult.NewVersion, _appDeployOptions.AppImageName);
        Logger.LogInformation("App deployed. AppId: {appId}, Version: {version}, RulePath: {rulePath}", appId, addResult.NewVersion, rulePath);
        return addResult.NewVersion;
    }

    public async Task UpdateSubscriptionManifestAsync(string appId, string version, SubscriptionManifestDto manifest)
    {
        await CheckAppExistAsync(appId);
        
        var appSubscriptionGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));

        var subscription = ObjectMapper.Map<SubscriptionManifestDto, SubscriptionManifest>(manifest);
        var currentSubscriptionInfos = await appSubscriptionGrain.GetSubscriptionAsync(version);
        
        //Check input subscription info if is valid
        CheckInputSubscriptionInfoIsValid(subscription.SubscriptionItems, currentSubscriptionInfos.SubscriptionItems);
        CheckInputSubscriptionInfoIsDuplicateOrMissing(subscription.SubscriptionItems,currentSubscriptionInfos.SubscriptionItems);

        await appSubscriptionGrain.UpdateSubscriptionAsync(version, subscription);
        await _appDeployManager.RestartAppAsync(appId, version);
    }

    public async Task UpdateSubscriptionCodeAsync(string appId, string version, byte[] code)
    {
        await CheckAppExistAsync(appId);
        CheckCode(code);
        
        var subscriptionGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        await subscriptionGrain.UpdateCodeAsync(version, code);
        await _appDeployManager.RestartAppAsync(appId, version);
        Logger.LogInformation("App updated. AppId: {appId}, Version: {version}", appId, version);
    }

    public async Task<AllSubscriptionDto> GetSubscriptionManifestAsync(string appId)
    {
        await CheckAppExistAsync(appId);
        
        var clientGrain =
            _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var allSubscription = await clientGrain.GetAllSubscriptionAsync();
        return ObjectMapper.Map<AllSubscription, AllSubscriptionDto>(allSubscription);
    }
    
    public async Task<List<SubscriptionIndexDto>> GetSubscriptionManifestIndexAsync(string appId)
    {
        var queryable = await _subscriptionIndexRepository.GetQueryableAsync();
        var subscriptions = queryable.Where(o => o.AppId == appId).ToList();
        return ObjectMapper.Map<List<AppSubscriptionIndex>, List<SubscriptionIndexDto>>(subscriptions);
    }

    private async Task CheckAppExistAsync(string appId)
    {
        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(appId));
        var app = await appGrain.GetAsync();
        if (app.AppId.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException("App does not exist.");
        }
    }

    private void AuditCode(byte[] code)
    {
        try
        {
            _codeAuditor.Audit(code);
        }
        catch (CodeCheckException ex)
        {
            throw new UserFriendlyException(ex.Message);
        }
    }
    
    private void CheckInputSubscriptionInfoIsValid(List<Subscription> subscriptionInfos,
        List<Subscription> currentSubscriptionInfos)
    {
        foreach (var subscriptionInfo in subscriptionInfos)
        {
            var currentSubscriptionInfoForCheckChainId = currentSubscriptionInfos.FindAll(i =>
                (i.ChainId == subscriptionInfo.ChainId));
            if (currentSubscriptionInfoForCheckChainId == null || currentSubscriptionInfoForCheckChainId.Count == 0)
            {
                var errorMessage = $"Invalid chain id {subscriptionInfo.ChainId}, can not add new chain";
                throw new UserFriendlyException("Invalid subscriptionInfo", details: errorMessage);
            }

            var currentSubscriptionInfo = currentSubscriptionInfoForCheckChainId.FirstOrDefault();
            if ((currentSubscriptionInfo.TransactionConditions == null ||
                 currentSubscriptionInfo.TransactionConditions.Count == 0) &&
                (subscriptionInfo.TransactionConditions != null && subscriptionInfo.TransactionConditions.Count > 0))
            {
                var errorMessage = $"Can not add transactionConditions in chain {subscriptionInfo.ChainId}";
                throw new UserFriendlyException("Invalid subscriptionInfo", details: errorMessage);
            }
            
            if ((currentSubscriptionInfo.LogEventConditions == null ||
                 currentSubscriptionInfo.LogEventConditions.Count == 0) &&
                (subscriptionInfo.LogEventConditions != null && subscriptionInfo.LogEventConditions.Count > 0))
            {
                var errorMessage = $"Can not add logEventConditions in chain {subscriptionInfo.ChainId}";
                throw new UserFriendlyException("Invalid subscriptionInfo", details: errorMessage);
            }
            
            var currentSubscriptionInfoForCheckStartBlockNumber = currentSubscriptionInfoForCheckChainId.FindAll(i =>
                i.StartBlockNumber == subscriptionInfo.StartBlockNumber);
            if (currentSubscriptionInfoForCheckStartBlockNumber == null || currentSubscriptionInfoForCheckStartBlockNumber.Count == 0)
            {
                var errorMessage =
                    $"Invalid start block number {subscriptionInfo.StartBlockNumber}, can not update start block number in chain {subscriptionInfo.ChainId}";
                throw new UserFriendlyException("Invalid subscriptionInfo", details: errorMessage);
            }
            
            var currentSubscriptionForCheckIsOnlyConfirmed = currentSubscriptionInfoForCheckStartBlockNumber.FindAll(i =>
                i.OnlyConfirmed == subscriptionInfo.OnlyConfirmed);
            if (currentSubscriptionForCheckIsOnlyConfirmed == null || currentSubscriptionForCheckIsOnlyConfirmed.Count == 0)
            {
                var errorMessage =
                    $"Invalid only confirmed block {subscriptionInfo.OnlyConfirmed}, can not update only confirmed block in chain {subscriptionInfo.ChainId}";
                throw new UserFriendlyException("Invalid subscriptionInfo", details: errorMessage);
            }
        }
    }

    private void CheckInputSubscriptionInfoIsDuplicateOrMissing(List<Subscription> subscriptionInfos,
        List<Subscription> currentSubscriptionInfos)
    {
        foreach (var currentSubscriptionInfo in currentSubscriptionInfos)
        {
            var subscriptionInfoForCheckDuplicate= subscriptionInfos.FindAll(i =>
                (i.ChainId == currentSubscriptionInfo.ChainId));
            if (subscriptionInfoForCheckDuplicate != null && subscriptionInfoForCheckDuplicate.Count > 1)
            {
                var errorMessage =
                    $"Duplicate subscribe information in chain {currentSubscriptionInfo.ChainId}";
                throw new UserFriendlyException("Invalid subscriptionInfo", details: errorMessage);
            }
            
            var subscriptionInfoForCheck = subscriptionInfoForCheckDuplicate.FirstOrDefault(i =>
                (i.StartBlockNumber == currentSubscriptionInfo.StartBlockNumber && i.OnlyConfirmed == currentSubscriptionInfo.OnlyConfirmed));
            if (subscriptionInfoForCheck == null)
            {
                var errorMessage =
                    $"Can not modify StartBlockNumber or OnlyConfirmed of subscribe information in chain {currentSubscriptionInfo.ChainId}";
                throw new UserFriendlyException("Invalid subscriptionInfo", details: errorMessage);
            }
            
            if (currentSubscriptionInfo.TransactionConditions != null &&
                currentSubscriptionInfo.TransactionConditions.Count > 0)
            {
                foreach (var transactionCondition in currentSubscriptionInfo.TransactionConditions)
                {
                    var currentTo = transactionCondition.To;
                    var subscriptionInfoForCheckTransactionCondition =
                        subscriptionInfoForCheck.TransactionConditions.FirstOrDefault(i =>
                            (i.To == currentTo));
                    if (subscriptionInfoForCheckTransactionCondition == null)
                    {
                        var errorMessage =
                            $"Can not remove subscribed transaction condition of to address {currentTo} in chain {subscriptionInfoForCheck.ChainId}";
                        throw new UserFriendlyException("Invalid subscriptionInfo", details: errorMessage);
                    }

                    foreach (var methodName in transactionCondition.MethodNames)
                    {
                        var inputMethodName=subscriptionInfoForCheckTransactionCondition.MethodNames.FirstOrDefault(i => i == methodName);
                        if (inputMethodName.IsNullOrEmpty())
                        {
                            var errorMessage =
                                $"Can not remove subscribed transaction condition of method name {methodName} in chain {subscriptionInfoForCheck.ChainId} to address {currentTo}";
                            throw new UserFriendlyException("Invalid subscriptionInfo", details: errorMessage);
                        }
                    }
                }
            }
            
            if (currentSubscriptionInfo.LogEventConditions != null &&
                currentSubscriptionInfo.LogEventConditions.Count > 0)
            {
                foreach (var logEventCondition in currentSubscriptionInfo.LogEventConditions)
                {
                    var currentContractAddress = logEventCondition.ContractAddress;
                    var subscriptionInfoForCheckLogEventCondition =
                        subscriptionInfoForCheck.LogEventConditions.FirstOrDefault(i =>
                            (i.ContractAddress == currentContractAddress));
                    if (subscriptionInfoForCheckLogEventCondition == null)
                    {
                        var errorMessage =
                            $"Can not remove subscribe log event condition of contract address {currentContractAddress} in chain {subscriptionInfoForCheck.ChainId}";
                        throw new UserFriendlyException("Invalid subscriptionInfo", details: errorMessage);
                    }

                    foreach (var eventName in logEventCondition.EventNames)
                    {
                        var inputEventName=subscriptionInfoForCheckLogEventCondition.EventNames.FirstOrDefault(i => i == eventName);
                        if (inputEventName.IsNullOrEmpty())
                        {
                            var errorMessage =
                                $"Can not remove subscribe log event condition of event name {eventName} in chain {subscriptionInfoForCheck.ChainId} contract address {currentContractAddress}";
                            throw new UserFriendlyException("Invalid subscriptionInfo", details: errorMessage);
                        }
                    }
                }
            }
        }
    }
    
    private void CheckCode(byte[] code)
    {
        if (code.Length > _appDeployOptions.MaxAppCodeSize)
        {
            throw new UserFriendlyException("Code is too Large.");
        }
        
        AuditCode(code);
    }
}