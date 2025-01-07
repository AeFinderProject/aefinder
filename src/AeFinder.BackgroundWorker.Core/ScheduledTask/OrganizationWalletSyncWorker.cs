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
    private readonly IOrganizationAppService _organizationAppService;

    public OrganizationWalletSyncWorker(AbpAsyncTimer timer,
        ILogger<OrganizationWalletSyncWorker> logger, 
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IOrganizationInformationProvider organizationInformationProvider,
        IUserAppService userAppService,
        IUserInformationProvider userInformationProvider,
        IOrganizationAppService organizationAppService,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _organizationInformationProvider = organizationInformationProvider;
        _userAppService = userAppService;
        _userInformationProvider = userInformationProvider;
        _organizationAppService = organizationAppService;
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
        var organizationUnitList = await _organizationAppService.GetAllOrganizationUnitsAsync();
        foreach (var organizationUnitDto in organizationUnitList)
        {
            var organizationId = organizationUnitDto.Id.ToString();
            var organizationName = organizationUnitDto.DisplayName;
            //Check organization wallet address is bind
            var organizationWalletAddress =
                await _organizationInformationProvider.GetOrganizationWalletAddressAsync(organizationId);
            if (!string.IsNullOrEmpty(organizationWalletAddress))
            {
                continue;
            }

            var users = await _userAppService.GetUsersInOrganizationUnitAsync(organizationUnitDto.Id);
            if (users == null)
            {
                _logger.LogWarning($"No users under the organization {organizationName}");
                continue;
            }

            var defaultUser = users.FirstOrDefault();
            if (defaultUser == null)
            {
                continue;
            }
            var userExtensionInfo = await _userInformationProvider.GetUserExtensionInfoByIdAsync(defaultUser.Id);
            if (string.IsNullOrEmpty(userExtensionInfo.WalletAddress))
            {
                _logger.LogWarning($"The user {defaultUser.Id} has not yet linked a wallet address.");
                continue;
            }

            organizationWalletAddress =
                await _organizationInformationProvider.GetUserOrganizationWalletAddressAsync(organizationId,
                    userExtensionInfo.WalletAddress);
            if (string.IsNullOrEmpty(organizationWalletAddress))
            {
                continue;
            }
            else
            {
                _logger.LogInformation($"Organization {organizationId} wallet address is bind.");
            }

        }

    }
}