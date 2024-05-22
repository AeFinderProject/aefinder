using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AeFinder.App.Deploy;
using AeFinder.Grains;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.Kubernetes.Manager;
using AeFinder.Studio;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace AeFinder.BlockScan;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class BlockScanAppService : AeFinderAppService, IBlockScanAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly IAppDeployManager _kubernetesAppManager;

    public BlockScanAppService(IClusterClient clusterClient, IAppDeployManager kubernetesAppManager)
    {
        _clusterClient = clusterClient;
        _kubernetesAppManager = kubernetesAppManager;
    }

    public async Task<string> AddSubscriptionAsync(string appId, SubscriptionManifestDto manifestDto, byte[] dll = null)
    {
        var dto = await AddSubscriptionV2Async(appId, manifestDto, dll);
        return dto.NewVersion;
    }

    public async Task<AddSubscriptionDto> AddSubscriptionV2Async(string appId, SubscriptionManifestDto manifestDto, byte[] dll = null)
    {
        Logger.LogInformation("ScanApp: {clientId} submit subscription: {subscription}", appId,
            JsonSerializer.Serialize(manifestDto));

        var subscription = ObjectMapper.Map<SubscriptionManifestDto, SubscriptionManifest>(manifestDto);
        var client = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        // TODO: Use the code from developer.
        if (dll == null)
        {
            return null;
        }

        return await client.AddSubscriptionV2Async(subscription, dll);
    }

    public async Task UpdateSubscriptionInfoAsync(string appId, string version,
        SubscriptionManifestDto manifestDto)
    {
        Logger.LogInformation($"Client: {appId} version: {version} update subscribe: {JsonSerializer.Serialize(manifestDto)}");
        var appSubscriptionGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));

        var subscription = ObjectMapper.Map<SubscriptionManifestDto, SubscriptionManifest>(manifestDto);
        var currentSubscriptionInfos = await appSubscriptionGrain.GetSubscriptionAsync(version);
        
        //TODO: Check input subscription info if is valid
        CheckInputSubscriptionInfoIsValid(subscription.SubscriptionItems, currentSubscriptionInfos.SubscriptionItems);
        
        
        
        await appSubscriptionGrain.UpdateSubscriptionAsync(version, subscription);
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

            // var currentSubscriptionInfo = subscriptionInfoForCheckChainId.FirstOrDefault();
            // if ((currentSubscriptionInfo.TransactionConditions == null ||
            //      currentSubscriptionInfo.TransactionConditions.Count == 0) &&
            //     (subscriptionInfo.TransactionConditions != null && subscriptionInfo.TransactionConditions.Count > 0))
            // {
            //     var errorMessage = $"Invalid chain id {subscriptionInfo.ChainId}, can not add transactionConditions";
            //     throw new UserFriendlyException("Invalid subscriptionInfo", details: errorMessage);
            // }
            //
            // if ((currentSubscriptionInfo.LogEventConditions == null ||
            //      currentSubscriptionInfo.LogEventConditions.Count == 0) &&
            //     (subscriptionInfo.LogEventConditions != null && subscriptionInfo.LogEventConditions.Count > 0))
            // {
            //     var errorMessage = $"Invalid chain id {subscriptionInfo.ChainId}, can not add logEventConditions";
            //     throw new UserFriendlyException("Invalid subscriptionInfo", details: errorMessage);
            // }
            
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
            
            var currentSubscriptionInfoForCheckTransactionConditions = currentSubscriptionForCheckIsOnlyConfirmed.FirstOrDefault();
            if (currentSubscriptionInfoForCheckTransactionConditions.TransactionConditions != null &&
                currentSubscriptionInfoForCheckTransactionConditions.TransactionConditions.Count > 0)
            {
                foreach (var transactionCondition in currentSubscriptionInfoForCheckTransactionConditions.TransactionConditions)
                {
                    var currentTo = transactionCondition.To;
                    var subscriptionInfoForCheckTransactionCondition =
                        subscriptionInfo.TransactionConditions.FirstOrDefault(i =>
                            (i.To == currentTo));
                    if (subscriptionInfoForCheckTransactionCondition == null)
                    {
                        var errorMessage =
                            $"Can not remove subscribe transaction condition to address {currentTo} in chain {subscriptionInfo.ChainId}";
                        throw new UserFriendlyException("Invalid subscriptionInfo", details: errorMessage);
                    }

                    foreach (var methodName in transactionCondition.MethodNames)
                    {
                        var inputMethodName=subscriptionInfoForCheckTransactionCondition.MethodNames.FirstOrDefault(i => i == methodName);
                        if (inputMethodName.IsNullOrEmpty())
                        {
                            var errorMessage =
                                $"Can not remove subscribe transaction condition method name {methodName} in chain {subscriptionInfo.ChainId} to address {currentTo}";
                            throw new UserFriendlyException("Invalid subscriptionInfo", details: errorMessage);
                        }
                    }
                }
                
            }
            
            
            var currentSubscriptionInfoForCheckLogEventConditions = currentSubscriptionForCheckIsOnlyConfirmed.FirstOrDefault();
            if (currentSubscriptionInfoForCheckLogEventConditions.LogEventConditions != null &&
                currentSubscriptionInfoForCheckLogEventConditions.LogEventConditions.Count > 0)
            {
                foreach (var logEventCondition in currentSubscriptionInfoForCheckLogEventConditions.LogEventConditions)
                {
                    var currentContractAddress = logEventCondition.ContractAddress;
                    var subscriptionInfoForCheckLogEventCondition =
                        subscriptionInfo.LogEventConditions.FirstOrDefault(i =>
                            (i.ContractAddress == currentContractAddress));
                    if (subscriptionInfoForCheckLogEventCondition == null)
                    {
                        var errorMessage =
                            $"Can not remove subscribe log event condition contract address {currentContractAddress} in chain {subscriptionInfo.ChainId}";
                        throw new UserFriendlyException("Invalid subscriptionInfo", details: errorMessage);
                    }

                    foreach (var eventName in logEventCondition.EventNames)
                    {
                        var inputEventName=subscriptionInfoForCheckLogEventCondition.EventNames.FirstOrDefault(i => i == eventName);
                        if (inputEventName.IsNullOrEmpty())
                        {
                            var errorMessage =
                                $"Can not remove subscribe transaction condition method name {eventName} in chain {subscriptionInfo.ChainId} contract address {currentContractAddress}";
                            throw new UserFriendlyException("Invalid subscriptionInfo", details: errorMessage);
                        }
                    }
                }
                
            }
            
        }
    }

    // public async Task UpdateSubscriptionInfoAsync(string clientId, string version, List<SubscriptionInfo> subscriptionInfos)
    // {
    //     Logger.LogInformation($"Client: {clientId} update subscribe: {JsonSerializer.Serialize(subscriptionInfos)}");
    //     
    //     var client = _clusterClient.GetGrain<IClientGrain>(clientId);
    //     
    //     //Check if the subscription info is valid
    //     var currentSubscriptionInfos = await client.GetSubscriptionAsync(version);
    //
    //     CheckInputSubscriptionInfoIsValid(subscriptionInfos, currentSubscriptionInfos);
    //
    //     CheckInputSubscriptionInfoIsDuplicateOrMissing(subscriptionInfos, currentSubscriptionInfos);
    //     
    //     //Update subscription info
    //     await client.UpdateSubscriptionInfoAsync(version, subscriptionInfos);
    //
    //     foreach (var subscriptionInfo in subscriptionInfos)
    //     {
    //         var id = GrainIdHelper.GenerateGrainId(subscriptionInfo.ChainId, clientId, version, subscriptionInfo.FilterType);
    //         var blockScanInfoGrain = _clusterClient.GetGrain<IBlockScanGrain>(id);
    //         await blockScanInfoGrain.UpdateSubscriptionInfoAsync(subscriptionInfo);
    //     }
    // }

    // private void CheckInputSubscriptionInfoIsValid(List<SubscriptionInfo> subscriptionInfos,
    //     List<SubscriptionInfo> currentSubscriptionInfos)
    // {
    //     foreach (var subscriptionInfo in subscriptionInfos)
    //     {
    //         var subscriptionInfoForCheckChainId = currentSubscriptionInfos.FindAll(i =>
    //             (i.ChainId == subscriptionInfo.ChainId));
    //         if (subscriptionInfoForCheckChainId == null || subscriptionInfoForCheckChainId.Count == 0)
    //         {
    //             var errorMessage = $"Invalid chain id {subscriptionInfo.ChainId}, can not add new chain";
    //             throw new UserFriendlyException("Invalid subscriptionInfo", details: errorMessage);
    //         }
    //
    //         var subscriptionInfoForCheckFilterType = subscriptionInfoForCheckChainId.FindAll(i =>
    //             i.FilterType == subscriptionInfo.FilterType);
    //         if (subscriptionInfoForCheckFilterType == null || subscriptionInfoForCheckFilterType.Count == 0)
    //         {
    //             continue;
    //         }
    //
    //         var subscriptionInfoForCheckStartBlockNumber = subscriptionInfoForCheckFilterType.FindAll(i =>
    //             i.StartBlockNumber == subscriptionInfo.StartBlockNumber);
    //         if (subscriptionInfoForCheckStartBlockNumber == null || subscriptionInfoForCheckStartBlockNumber.Count == 0)
    //         {
    //             var errorMessage =
    //                 $"Invalid start block number {subscriptionInfo.StartBlockNumber}, can not update start block number in chain {subscriptionInfo.ChainId} filterType {subscriptionInfo.FilterType}";
    //             throw new UserFriendlyException("Invalid subscriptionInfo", details: errorMessage);
    //         }
    //
    //         var subscriptionInfoForCheckIsOnlyConfirmed = subscriptionInfoForCheckStartBlockNumber.FindAll(i =>
    //             i.OnlyConfirmedBlock == subscriptionInfo.OnlyConfirmedBlock);
    //         if (subscriptionInfoForCheckIsOnlyConfirmed == null || subscriptionInfoForCheckIsOnlyConfirmed.Count == 0)
    //         {
    //             var errorMessage =
    //                 $"Invalid only confirmed block {subscriptionInfo.OnlyConfirmedBlock}, can not update only confirmed block in chain {subscriptionInfo.ChainId} filterType {subscriptionInfo.FilterType}";
    //             throw new UserFriendlyException("Invalid subscriptionInfo", details: errorMessage);
    //         }
    //
    //         var subscriptionInfoForCheckContract = subscriptionInfoForCheckIsOnlyConfirmed.FirstOrDefault();
    //         if (subscriptionInfo.SubscribeEvents == null || subscriptionInfo.SubscribeEvents.Count == 0)
    //         {
    //             if (subscriptionInfoForCheckContract.SubscribeEvents != null &&
    //                 subscriptionInfoForCheckContract.SubscribeEvents.Count > 0)
    //             {
    //                 var errorMessage =
    //                     $"Can not empty subscribe contracts in chain {subscriptionInfo.ChainId} filterType {subscriptionInfo.FilterType}";
    //                 throw new UserFriendlyException("Invalid subscriptionInfo", details: errorMessage);
    //             }
    //         }
    //         else
    //         {
    //             if (subscriptionInfoForCheckContract.SubscribeEvents == null ||
    //                 subscriptionInfoForCheckContract.SubscribeEvents.Count == 0)
    //             {
    //                 var errorMessage =
    //                     $"Can not add subscribe contracts in chain {subscriptionInfo.ChainId} filterType {subscriptionInfo.FilterType}";
    //                 throw new UserFriendlyException("Invalid subscriptionInfo", details: errorMessage);
    //             }
    //
    //             foreach (var filterContractEventInput in subscriptionInfo.SubscribeEvents)
    //             {
    //                 var subscriptionInfoForCheckContractEvent =
    //                     subscriptionInfoForCheckContract.SubscribeEvents.FirstOrDefault(i =>
    //                         (i.ContractAddress == filterContractEventInput.ContractAddress));
    //                 if (subscriptionInfoForCheckContractEvent == null)
    //                 {
    //                     var errorMessage =
    //                         $"Can not add new subscribe contract {filterContractEventInput.ContractAddress} in chain {subscriptionInfo.ChainId} filterType {subscriptionInfo.FilterType}";
    //                     throw new UserFriendlyException("Invalid subscriptionInfo", details: errorMessage);
    //                 }
    //             }
    //         }
    //     }
    // }
    //
    // private void CheckInputSubscriptionInfoIsDuplicateOrMissing(List<SubscriptionInfo> subscriptionInfos,
    //     List<SubscriptionInfo> currentSubscriptionInfos)
    // {
    //     foreach (var currentSubscriptionInfo in currentSubscriptionInfos)
    //     {
    //         var subscriptionInfoForCheckDuplicate= subscriptionInfos.FindAll(i =>
    //             (i.ChainId == currentSubscriptionInfo.ChainId && i.FilterType == currentSubscriptionInfo.FilterType));
    //         if (subscriptionInfoForCheckDuplicate != null && subscriptionInfoForCheckDuplicate.Count > 1)
    //         {
    //             var errorMessage =
    //                 $"Duplicate subscribe information in chain {currentSubscriptionInfo.ChainId} filterType {currentSubscriptionInfo.FilterType}";
    //             throw new UserFriendlyException("Invalid subscriptionInfo", details: errorMessage);
    //         }
    //         
    //         var subscriptionInfoForCheck = subscriptionInfoForCheckDuplicate.FirstOrDefault(i =>
    //             (i.StartBlockNumber == currentSubscriptionInfo.StartBlockNumber && i.OnlyConfirmedBlock == currentSubscriptionInfo.OnlyConfirmedBlock));
    //         if (subscriptionInfoForCheck == null)
    //         {
    //             var errorMessage =
    //                 $"Missing subscribe information in chain {currentSubscriptionInfo.ChainId} filterType {currentSubscriptionInfo.FilterType}";
    //             throw new UserFriendlyException("Invalid subscriptionInfo", details: errorMessage);
    //         }
    //
    //         if (currentSubscriptionInfo.SubscribeEvents != null && currentSubscriptionInfo.SubscribeEvents.Count > 0)
    //         {
    //             foreach (var currentSubscribeContract in currentSubscriptionInfo.SubscribeEvents)
    //             {
    //                 var subscriptionInfoForCheckContractEvent = subscriptionInfoForCheck.SubscribeEvents.FirstOrDefault(i =>
    //                     (i.ContractAddress == currentSubscribeContract.ContractAddress));
    //                 if (subscriptionInfoForCheckContractEvent == null)
    //                 {
    //                     var errorMessage =
    //                         $"Can not remove subscribe contract {currentSubscribeContract.ContractAddress} in chain {currentSubscriptionInfo.ChainId} filterType {currentSubscriptionInfo.FilterType}";
    //                     throw new UserFriendlyException("Invalid subscriptionInfo", details: errorMessage);
    //                 }
    //             }
    //         }
    //     }
    // }

    public async Task<List<Guid>> GetMessageStreamIdsAsync(string clientId, string version)
    {
        var client = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(clientId));
        var subscription = await client.GetSubscriptionAsync(version);
        var streamIds = new List<Guid>();
        foreach (var subscriptionItem in subscription.SubscriptionItems)
        {
            var id = GrainIdHelper.GenerateBlockPusherGrainId(clientId, version, subscriptionItem.ChainId);
            var blockScanInfoGrain = _clusterClient.GetGrain<IBlockPusherInfoGrain>(id);
            var streamId = await blockScanInfoGrain.GetMessageStreamIdAsync();
            streamIds.Add(streamId);
        }

        return streamIds;
    }

    public async Task StartScanAsync(string clientId, string version)
    {
        Logger.LogInformation("ScanApp: {clientId} start scan, version: {version}", clientId, version);

        var client = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(clientId));
        var subscription = await client.GetSubscriptionAsync(version);
        var versionStatus = await client.GetSubscriptionStatusAsync(version);
        var scanToken = Guid.NewGuid().ToString("N");
        await client.StartAsync(version);
        foreach (var subscriptionItem in subscription.SubscriptionItems)
        {
            var id = GrainIdHelper.GenerateBlockPusherGrainId(clientId, version, subscriptionItem.ChainId);
            var blockScanGrain = _clusterClient.GetGrain<IBlockPusherInfoGrain>(id);
            var blockScanExecutorGrain = _clusterClient.GetGrain<IBlockPusherGrain>(id);

            var startBlockHeight = subscriptionItem.StartBlockNumber;

            if (versionStatus != SubscriptionStatus.Initialized)
            {
                var appBlockStateSetStatusGrain = _clusterClient.GetGrain<IAppBlockStateSetStatusGrain>(
                    GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(clientId, version, subscriptionItem.ChainId));
                // TODO: This is not correct, we should get the confirmed block height from the chain grain.
                startBlockHeight = (await appBlockStateSetStatusGrain.GetBlockStateSetStatusAsync()).LastIrreversibleBlockHeight;
                if (startBlockHeight == 0)
                {
                    startBlockHeight = subscriptionItem.StartBlockNumber;
                }
                else
                {
                    startBlockHeight += 1;
                }
            }

            await blockScanGrain.InitializeAsync(clientId, version, subscriptionItem, scanToken);
            await blockScanExecutorGrain.InitializeAsync(scanToken, startBlockHeight);

            Logger.LogDebug("Start ScanApp: {clientId}, id: {id}", clientId, id);
            _ = Task.Run(blockScanExecutorGrain.HandleHistoricalBlockAsync);
        }
    }

    public async Task UpgradeVersionAsync(string clientId)
    {
        var client = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(clientId));
        await client.UpgradeVersionAsync();
    }

    public async Task<AllSubscriptionDto> GetSubscriptionAsync(string clientId)
    {
        var clientGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(clientId));
        var allSubscription = await clientGrain.GetAllSubscriptionAsync();
        return ObjectMapper.Map<AllSubscription, AllSubscriptionDto>(allSubscription);
    }

    public async Task PauseAsync(string clientId, string version)
    {
        var client = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(clientId));
        var scanManager = _clusterClient.GetGrain<IBlockPusherManagerGrain>(0);
        var subscription = await client.GetSubscriptionAsync(version);
        await client.PauseAsync(version);
        foreach (var subscriptionItem in subscription.SubscriptionItems)
        {
            var id = GrainIdHelper.GenerateBlockPusherGrainId(clientId, version, subscriptionItem.ChainId);
            await scanManager.RemoveBlockPusherAsync(subscriptionItem.ChainId, id);
        }
    }

    public async Task StopAsync(string clientId, string version)
    {
        var clientGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(clientId));
        Logger.LogDebug("ScanApp: {clientId} stop scan, version: {version}", clientId, version);
        await clientGrain.StopAsync(version);
        Logger.LogDebug("ScanApp: {clientId} stop scan, version: {version}", clientId, version);
        await _kubernetesAppManager.DestroyAppAsync(clientId, version);
    }

    public async Task<bool> IsRunningAsync(string chainId, string clientId, string version, string token)
    {
        var clientGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(clientId));
        return await clientGrain.IsRunningAsync(version, chainId, token);
    }
}