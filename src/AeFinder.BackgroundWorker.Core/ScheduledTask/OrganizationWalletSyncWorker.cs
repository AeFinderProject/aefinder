using AeFinder.BackgroundWorker.Options;
using AeFinder.User;
using AeFinder.User.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace AeFinder.BackgroundWorker.ScheduledTask;

public class OrganizationWalletSyncWorker: AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<OrganizationWalletSyncWorker> _logger;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;
    private readonly IOrganizationInformationProvider _organizationInformationProvider;
    private readonly IUserAppService _userAppService;
    private readonly IUserInformationProvider _userInformationProvider;

    public OrganizationWalletSyncWorker(AbpAsyncTimer timer,
        ILogger<OrganizationWalletSyncWorker> logger, 
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IOrganizationInformationProvider organizationInformationProvider,
        IUserAppService userAppService,
        IUserInformationProvider userInformationProvider,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _organizationInformationProvider = organizationInformationProvider;
        _userAppService = userAppService;
        _userInformationProvider = userInformationProvider;
        Timer.Period = _scheduledTaskOptions.BillingIndexerPollingTaskPeriodMilliSeconds;
    }
    
    
    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await ProcessSynchronizationAsync();
    }

    private async Task ProcessSynchronizationAsync()
    {
        //Get no wallet organizations
        var organizationList = await _organizationInformationProvider.GetOrganizationWithoutWalletListAsync();
        foreach (var organizationId in organizationList)
        {
            //Check organization wallet address is bind
            var organizationWalletAddress =
                await _organizationInformationProvider.GetOrganizationWalletAddressAsync(organizationId.ToString());
            if (string.IsNullOrEmpty(organizationWalletAddress))
            {
                var users = await _userAppService.GetUsersInOrganizationUnitAsync(organizationId);
                if (users == null)
                {
                    _logger.LogWarning($"No users under the organization {organizationId.ToString()}");
                    continue;
                }

                var defaultUser = users.FirstOrDefault();
                var userExtensionInfo = await _userInformationProvider.GetUserExtensionInfoByIdAsync(defaultUser.Id);
                if (string.IsNullOrEmpty(userExtensionInfo.WalletAddress))
                {
                    _logger.LogWarning($"The user {defaultUser.Id} has not yet linked a wallet address.");
                    continue;
                }

                organizationWalletAddress =
                    await _organizationInformationProvider.GetUserOrganizationWalletAddressAsync(organizationId.ToString(),
                        userExtensionInfo.WalletAddress);
                if (string.IsNullOrEmpty(organizationWalletAddress))
                {
                    _logger.LogWarning($"Organization {organizationId} wallet address is null or empty, please check.");
                    continue;
                }
            }
        }

    }
}