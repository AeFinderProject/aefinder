using AElfIndexer.BlockScan;
using AElfIndexer.Grains.State.BlockScan;
using Orleans;

namespace AElfIndexer.Grains.Grain.BlockScan;

public class ClientGrain : Grain<ClientState>, IClientGrain
{
    public async Task<string> AddSubscriptionInfoAsync(List<SubscriptionInfo> subscriptionInfos)
    {
        var newVersion = Guid.NewGuid().ToString("N");
        State.VersionInfos[newVersion] = new ClientVersionInfo
        {
            SubscriptionInfos = subscriptionInfos,
            BlockScanIds = new HashSet<string>(),
            VersionStatus = VersionStatus.Initialized
        };
        
        if (string.IsNullOrWhiteSpace(State.CurrentVersion))
        {
            State.CurrentVersion = newVersion;
        }
        else
        {
            State.NewVersion = newVersion;
        }

        return newVersion;
        
    }

    public async Task<List<SubscriptionInfo>> GetSubscriptionInfoAsync(string version)
    {
        return State.VersionInfos[version].SubscriptionInfos;
    }

    public async Task AddBlockScanIdAsync(string version, string id)
    {
        if (version != State.NewVersion && version != State.CurrentVersion)
        {
            return;
        }

        State.VersionInfos[version].BlockScanIds.Add(id);
        await WriteStateAsync();
    }

    public async Task<List<string>> GetBlockScanIdsAsync(string version)
    {
        if (State.VersionInfos.TryGetValue(version, out var clientVersionInfo))
        {
            return clientVersionInfo.BlockScanIds.ToList();
        }

        return new List<string>();
    }

    public async Task<bool> IsVersionAvailableAsync(string version)
    {
        return version == State.NewVersion || version == State.CurrentVersion;
    }

    public async Task UpgradeVersionAsync()
    {
        if (State.CurrentVersion == State.NewVersion || string.IsNullOrWhiteSpace(State.NewVersion))
        {
            return;
        }

        State.VersionInfos.Remove(State.CurrentVersion);
        State.CurrentVersion = State.NewVersion;
        State.NewVersion = null;
        await WriteStateAsync();
    }
    
    public async Task RemoveVersionInfoAsync(string version)
    {
        if (State.CurrentVersion == version)
        {
            return;
        }

        State.VersionInfos.Remove(version);
        await WriteStateAsync();
    }

    public async Task<VersionStatus> GetVersionStatusAsync(string version)
    {
        return State.VersionInfos[version].VersionStatus;
    }

    public async Task StartAsync(string version)
    {
        State.VersionInfos[version].VersionStatus = VersionStatus.Started;
        await WriteStateAsync();
    }

    public async Task<ClientVersion> GetVersionAsync()
    {
        return new ClientVersion
        {
            CurrentVersion = State.CurrentVersion,
            NewVersion = State.NewVersion
        };
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
    }
}