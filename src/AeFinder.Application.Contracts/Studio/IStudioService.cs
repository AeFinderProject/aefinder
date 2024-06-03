using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.BlockScan;

namespace AeFinder.Studio;

public interface IStudioService
{
    Task<AppBlockStateMonitorDto> MonitorAppBlockStateAsync(string appId);
}