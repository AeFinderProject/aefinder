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

        await WriteStateAsync();
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

    public async Task<bool> IsVersionRunningAsync(string version, string token)
    {
        return !string.IsNullOrWhiteSpace(version) && 
               (version == State.NewVersion || version == State.CurrentVersion) &&
               State.VersionInfos[version].VersionStatus == VersionStatus.Started &&
               State.TokenInfos[version] == token;
    }

    public async Task UpgradeVersionAsync()
    {
        if (string.IsNullOrWhiteSpace(State.NewVersion))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(State.CurrentVersion))
        {
            State.VersionInfos.Remove(State.CurrentVersion);
        }

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
    
    public async Task PauseAsync(string version)
    {
        State.VersionInfos[version].VersionStatus = VersionStatus.Paused;
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

    public async Task SetTokenAsync(string version)
    {
        var token = Guid.NewGuid().ToString("N");
        State.TokenInfos[version] = token;
        await WriteStateAsync();
    }

    public async Task<string> GetTokenAsync(string version)
    {
        if (State.TokenInfos.TryGetValue(version, out var token))
        {
            return token;
        }

        return null;;
    }

    public async Task StopAsync(string version)
    {
        State.VersionInfos.Remove(version);
        if (State.CurrentVersion == version)
        {
            State.CurrentVersion = null;
        }
        
        if (State.NewVersion == version)
        {
            State.NewVersion = null;
        }
        
        await WriteStateAsync();
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
    }
}