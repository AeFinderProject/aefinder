using System;
using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Billings;
using MongoDB.Driver.Linq;
using Orleans;
using Shouldly;
using Xunit;

namespace AeFinder.Billings;

public class BillingManagementServiceTests : AeFinderApplicationTestBase
{
    private readonly IBillingManagementService _billingManagementService;
    private readonly IClusterClient _clusterClient;
    private readonly IBillingService _billingService;

    public BillingManagementServiceTests()
    {
        _billingService = GetRequiredService<IBillingService>();
        _billingManagementService = GetRequiredService<IBillingManagementService>();
        _clusterClient = GetRequiredService<IClusterClient>();
    }

    [Fact]
    public async Task GenerateMonthlyBilling_Test()
    {
        var date = DateTime.UtcNow;
        await _billingManagementService.GenerateMonthlyBillingAsync(AeFinderApplicationTestConsts.OrganizationId, date);

        var monthBillingGrain =
            _clusterClient.GetGrain<IMonthlyBillingGrain>(
                GrainIdHelper.GenerateMonthlyBillingGrainId(AeFinderApplicationTestConsts.OrganizationId, date));
        var monthBilling = await monthBillingGrain.GetAsync();
        monthBilling.SettlementBillingId.ShouldNotBe(Guid.Empty);
        monthBilling.AdvancePaymentBillingId.ShouldBe(Guid.Empty);
        monthBilling.OrganizationId.ShouldBe(AeFinderApplicationTestConsts.OrganizationId);
        monthBilling.BillingDate.ShouldBe(date.ToMonthDate());

        var billingGrain = _clusterClient.GetGrain<IBillingGrain>(monthBilling.SettlementBillingId);
        var billingSettlement = await billingGrain.GetAsync();
        billingSettlement.Id.ShouldBe(monthBilling.SettlementBillingId);
        billingSettlement.BeginTime.ShouldBe(date.ToMonthDate());

        await _billingManagementService.GenerateMonthlyBillingAsync(AeFinderApplicationTestConsts.OrganizationId, date);
        monthBilling = await monthBillingGrain.GetAsync();
        monthBilling.SettlementBillingId.ShouldNotBe(Guid.Empty);
        monthBilling.AdvancePaymentBillingId.ShouldBe(Guid.Empty);

        await billingGrain.PayAsync("txId", DateTime.UtcNow);

        await _billingManagementService.GenerateMonthlyBillingAsync(AeFinderApplicationTestConsts.OrganizationId, date);
        monthBilling = await monthBillingGrain.GetAsync();
        monthBilling.SettlementBillingId.ShouldNotBe(Guid.Empty);
        monthBilling.AdvancePaymentBillingId.ShouldBe(Guid.Empty);

        await billingGrain.PaymentFailedAsync();

        await _billingManagementService.GenerateMonthlyBillingAsync(AeFinderApplicationTestConsts.OrganizationId, date);
        monthBilling = await monthBillingGrain.GetAsync();
        monthBilling.SettlementBillingId.ShouldNotBe(Guid.Empty);
        monthBilling.AdvancePaymentBillingId.ShouldBe(Guid.Empty);

        await billingGrain.ConfirmPaymentAsync();

        await _billingManagementService.GenerateMonthlyBillingAsync(AeFinderApplicationTestConsts.OrganizationId, date);
        monthBilling = await monthBillingGrain.GetAsync();
        monthBilling.SettlementBillingId.ShouldNotBe(Guid.Empty);
        monthBilling.AdvancePaymentBillingId.ShouldNotBe(Guid.Empty);

        billingGrain = _clusterClient.GetGrain<IBillingGrain>(monthBilling.AdvancePaymentBillingId);
        var billingAdvancePayment = await billingGrain.GetAsync();
        billingAdvancePayment.Id.ShouldBe(monthBilling.AdvancePaymentBillingId);
        billingAdvancePayment.BeginTime.ShouldBe(date.ToMonthDate().AddMonths(1));

        await _billingManagementService.GenerateMonthlyBillingAsync(AeFinderApplicationTestConsts.OrganizationId, date);
        monthBilling = await monthBillingGrain.GetAsync();
        monthBilling.SettlementBillingId.ShouldBe(billingSettlement.Id);
        monthBilling.AdvancePaymentBillingId.ShouldBe(billingAdvancePayment.Id);
    }

    [Fact]
    public async Task GenerateMonthlyBilling_Compatibility_Test()
    {
        var date = DateTime.UtcNow;

        var billingSettlement = await _billingService.CreateAsync(AeFinderApplicationTestConsts.OrganizationId,
            BillingType.Settlement, date);
        await _billingService.UpdateIndexAsync(billingSettlement.Id);

        await _billingManagementService.GenerateMonthlyBillingAsync(AeFinderApplicationTestConsts.OrganizationId, date);

        var monthBillingGrain =
            _clusterClient.GetGrain<IMonthlyBillingGrain>(
                GrainIdHelper.GenerateMonthlyBillingGrainId(AeFinderApplicationTestConsts.OrganizationId, date));
        var monthBilling = await monthBillingGrain.GetAsync();
        monthBilling.SettlementBillingId.ShouldBe(billingSettlement.Id);
        monthBilling.AdvancePaymentBillingId.ShouldBe(Guid.Empty);
        monthBilling.OrganizationId.ShouldBe(AeFinderApplicationTestConsts.OrganizationId);
        monthBilling.BillingDate.ShouldBe(date.ToMonthDate());

        await _billingService.ConfirmPaymentAsync(billingSettlement.Id);

        var billingAdvancePayment = await _billingService.CreateAsync(AeFinderApplicationTestConsts.OrganizationId,
            BillingType.AdvancePayment, date.AddMonths(1));
        await _billingService.UpdateIndexAsync(billingAdvancePayment.Id);
        
        await _billingManagementService.GenerateMonthlyBillingAsync(AeFinderApplicationTestConsts.OrganizationId, date);

        monthBilling = await monthBillingGrain.GetAsync();
        monthBilling.SettlementBillingId.ShouldBe(billingSettlement.Id);
        monthBilling.AdvancePaymentBillingId.ShouldBe(billingAdvancePayment.Id);
        monthBilling.OrganizationId.ShouldBe(AeFinderApplicationTestConsts.OrganizationId);
        monthBilling.BillingDate.ShouldBe(date.ToMonthDate());
    }

    [Fact]
    public async Task Pay_Test()
    {
        var date = DateTime.UtcNow;
        var billingSettlement = await _billingService.CreateAsync(AeFinderApplicationTestConsts.OrganizationId,
            BillingType.Settlement, date);

        await _billingManagementService.PayAsync(billingSettlement.Id);
        var billingGrain = _clusterClient.GetGrain<IBillingGrain>(billingSettlement.Id);
        var billing = await billingGrain.GetAsync();
        billing.Status.ShouldBe(BillingStatus.Paid);
        
        await _billingManagementService.PayAsync(billingSettlement.Id);
        billing = await billingGrain.GetAsync();
        billing.Status.ShouldBe(BillingStatus.Paid);

        await _billingService.PaymentFailedAsync(billingSettlement.Id);
        await _billingManagementService.PayAsync(billingSettlement.Id);
        billing = await billingGrain.GetAsync();
        billing.Status.ShouldBe(BillingStatus.Failed);
    }

    [Fact]
    public async Task RePay_Test()
    {
        var date = DateTime.UtcNow;
        var billingSettlement = await _billingService.CreateAsync(AeFinderApplicationTestConsts.OrganizationId,
            BillingType.Settlement, date);
        await _billingService.PaymentFailedAsync(billingSettlement.Id);

        await _billingManagementService.RePayAsync(billingSettlement.Id);

        var billingGrain = _clusterClient.GetGrain<IBillingGrain>(billingSettlement.Id);
        var billing = await billingGrain.GetAsync();
        billing.Status.ShouldBe(BillingStatus.Paid);

        var billingAdvancePayment = await _billingService.CreateAsync(AeFinderApplicationTestConsts.OrganizationId,
            BillingType.AdvancePayment, date.AddMonths(1));
        await _billingService.PaymentFailedAsync(billingAdvancePayment.Id);

        await _billingManagementService.RePayAsync(billingAdvancePayment.Id);

        billingGrain = _clusterClient.GetGrain<IBillingGrain>(billingAdvancePayment.Id);
        billing = await billingGrain.GetAsync();
        billing.Status.ShouldBe(BillingStatus.Paid);
    }

    [Fact]
    public async Task IsPaymentFailed_Test()
    {
        var date = DateTime.UtcNow;
        var isPaymentFailed =
            await _billingManagementService.IsPaymentFailedAsync(AeFinderApplicationTestConsts.OrganizationId, date);
        isPaymentFailed.ShouldBeFalse();

        await _billingManagementService.GenerateMonthlyBillingAsync(AeFinderApplicationTestConsts.OrganizationId, date);

        isPaymentFailed =
            await _billingManagementService.IsPaymentFailedAsync(AeFinderApplicationTestConsts.OrganizationId, date);
        isPaymentFailed.ShouldBeFalse();

        var monthBillingGrain =
            _clusterClient.GetGrain<IMonthlyBillingGrain>(
                GrainIdHelper.GenerateMonthlyBillingGrainId(AeFinderApplicationTestConsts.OrganizationId, date));
        var monthBilling = await monthBillingGrain.GetAsync();
        await _billingService.ConfirmPaymentAsync(monthBilling.SettlementBillingId);

        await _billingManagementService.GenerateMonthlyBillingAsync(AeFinderApplicationTestConsts.OrganizationId, date);

        isPaymentFailed =
            await _billingManagementService.IsPaymentFailedAsync(AeFinderApplicationTestConsts.OrganizationId, date);
        isPaymentFailed.ShouldBeFalse();

        monthBilling = await monthBillingGrain.GetAsync();
        await _billingService.ConfirmPaymentAsync(monthBilling.AdvancePaymentBillingId);

        isPaymentFailed =
            await _billingManagementService.IsPaymentFailedAsync(AeFinderApplicationTestConsts.OrganizationId, date);
        isPaymentFailed.ShouldBeFalse();

        monthBilling = await monthBillingGrain.GetAsync();
        await _billingService.PaymentFailedAsync(monthBilling.AdvancePaymentBillingId);

        isPaymentFailed =
            await _billingManagementService.IsPaymentFailedAsync(AeFinderApplicationTestConsts.OrganizationId, date);
        isPaymentFailed.ShouldBeTrue();
    }
}