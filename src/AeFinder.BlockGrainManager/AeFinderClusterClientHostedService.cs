using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Threading;

namespace AeFinder.BlockGrainManager;

public class AeFinderClusterClientHostedService:IHostedService
{
    private readonly IAbpApplicationWithExternalServiceProvider _application;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly BlockGrainManagerOptions _blockGrainManagerOptions;
    
    public AeFinderClusterClientHostedService(
        IAbpApplicationWithExternalServiceProvider application,
        IServiceProvider serviceProvider,IConfiguration configuration,
        IOptionsSnapshot<BlockGrainManagerOptions> blockGrainManagerOptions)
    {
        _application = application;
        _serviceProvider = serviceProvider;
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _blockGrainManagerOptions = blockGrainManagerOptions.Value;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _application.Initialize(_serviceProvider);
        // string settingValue = _configuration["MyConfig:SettingValue"];
        Console.WriteLine("Please confirm the chain id:" + _blockGrainManagerOptions.ChainId);
        Console.WriteLine("Please confirm this sync start height:" + _blockGrainManagerOptions.StartHeight);
        Console.WriteLine("Please confirm this sync end height:" + _blockGrainManagerOptions.EndHeight);
        Console.WriteLine("if you want to continue, please input 'y' or 'Y'");
        var command = Console.ReadLine();
        if (command != "y" && command != "Y")
        {
            Console.WriteLine("You have canceled the operation.");
            return Task.CompletedTask;
        }
        
        AsyncHelper.RunSync(async () =>
        {
            var blockDataSynchronizer = (IBlockDataSynchronizer)_serviceProvider.GetService(typeof(IBlockDataSynchronizer));
            await blockDataSynchronizer.SyncConfirmedBlockAsync(_blockGrainManagerOptions.ChainId,
                _blockGrainManagerOptions.StartHeight, _blockGrainManagerOptions.EndHeight);
        });

        StopAsync(cancellationToken);
        return Task.CompletedTask;
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _application.Shutdown();
        return Task.CompletedTask;
    }
}