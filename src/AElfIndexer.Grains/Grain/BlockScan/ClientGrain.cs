using AElfIndexer.BlockScan;
using AElfIndexer.Grains.State.BlockScan;
using Orleans;

namespace AElfIndexer.Grains.Grain.BlockScan;

public class ClientGrain : Grain<ClientState>, IClientGrain
{
    public async Task<string> AddSubscribeInfoAsync(List<SubscribeInfo> subscribeInfos)
    {
        var newVersion = Guid.NewGuid().ToString("N");
        State.VersionInfos[newVersion] = new ClientVersionInfo
        {
            SubscribeInfos = subscribeInfos,
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

    public async Task<List<SubscribeInfo>> GetSubscribeInfoAsync(string version)
    {
        return State.VersionInfos[version].SubscribeInfos;
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
        return version != State.NewVersion || version != State.CurrentVersion;
    }

    public async Task<string> GetCurrentVersionAsync()
    {
        return State.CurrentVersion;
    }

    public async Task<string> GetNewVersionAsync()
    {
        return State.NewVersion;
    }

    public async Task UpgradeVersionAsync()
    {
        if (State.CurrentVersion == State.NewVersion || string.IsNullOrWhiteSpace(State.NewVersion))
        {
            return;
        }

        State.VersionInfos.Remove(State.CurrentVersion);
        State.CurrentVersion = State.NewVersion;
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

    public async Task<VersionStatus> GetVersionStatus(string version)
    {
        return State.VersionInfos[version].VersionStatus;
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
    }
}