using System;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Assets;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Assets;
using AeFinder.Grains.Grain.Billings;
using AeFinder.Grains.State.Assets;
using AeFinder.Grains.State.Billings;
using AeFinder.Merchandises;
using Orleans;
using Shouldly;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace AeFinder.Billings;

public class BillingServiceTests : AeFinderApplicationTestBase
{
    private readonly IClusterClient _clusterClient;
    private readonly IBillingService _billingService;
    private readonly IMerchandiseService _merchandiseService;
    private readonly IAssetService _assetService;
    private readonly IObjectMapper _objectMapper;

    public BillingServiceTests()
    {
        _billingService = GetRequiredService<IBillingService>();
        _clusterClient = GetRequiredService<IClusterClient>();
        _merchandiseService = GetRequiredService<IMerchandiseService>();
        _assetService = GetRequiredService<IAssetService>();
        _objectMapper = GetRequiredService<IObjectMapper>();
    }

    [Fact]
    public async Task BillingTest()
    {
        var merchandises = await _merchandiseService.GetListAsync(new GetMerchandiseInput
        {
            Type = MerchandiseType.Processor
        });
        
        var assetId = Guid.NewGuid();
        var assetGrain = _clusterClient.GetGrain<IAssetGrain>(assetId);
        await assetGrain.CreateAssetAsync(assetId, AeFinderApplicationTestConsts.OrganizationId,new CreateAssetInput
        {
            MerchandiseId = merchandises.Items.First().Id,
            Quantity = 100,
            Replicas = 1,
            CreateTime = DateTime.UtcNow
        });
        var startTime = DateTime.UtcNow;
        await assetGrain.StartUsingAsync(startTime);
        await assetGrain.PayAsync(10000);
        var asset = await assetGrain.GetAsync();
        await _assetService.AddOrUpdateIndexAsync(_objectMapper.Map<AssetState, AssetChangedEto>(asset));

        var settlementBilling = await _billingService.CreateAsync(AeFinderApplicationTestConsts.OrganizationId,
            BillingType.Settlement, DateTime.UtcNow);
        
        var billingGrain = _clusterClient.GetGrain<IBillingGrain>(settlementBilling.Id);
        var billingState = await billingGrain.GetAsync();
        
        var amount = (long)Math.Ceiling((startTime.ToMonthDate().AddMonths(1) - startTime).TotalHours) *
                     merchandises.Items.First().Price;
        billingState.PaidAmount.ShouldBe(amount);
        billingState.RefundAmount.ShouldBe(10000 - amount);
        
        var payTime = DateTime.UtcNow;
        await _billingService.PayAsync(settlementBilling.Id, "txid", payTime);

        billingState = await billingGrain.GetAsync();
        billingState.Status.ShouldBe(BillingStatus.Confirming);
        billingState.TransactionId.ShouldBe("txid");
        billingState.PaymentTime.ShouldBe(payTime);

        await _billingService.ConfirmPaymentAsync(settlementBilling.Id);
        billingState = await billingGrain.GetAsync();
        billingState.Status.ShouldBe(BillingStatus.Paid);

        await _billingService.AddOrUpdateIndexAsync(_objectMapper.Map<BillingState, BillingChangedEto>(billingState));
        
        var advancePaymentBilling = await _billingService.CreateAsync(AeFinderApplicationTestConsts.OrganizationId,
            BillingType.AdvancePayment, DateTime.UtcNow.AddMonths(1));
        amount = (long)Math.Ceiling((startTime.ToMonthDate().AddMonths(2) - DateTime.UtcNow.AddMonths(1).ToMonthDate()).TotalHours) *
                     merchandises.Items.First().Price;
        
        billingGrain = _clusterClient.GetGrain<IBillingGrain>(advancePaymentBilling.Id);
        billingState = await billingGrain.GetAsync();
        
        billingState.PaidAmount.ShouldBe(amount);
        billingState.RefundAmount.ShouldBe(0);

        await _billingService.AddOrUpdateIndexAsync(_objectMapper.Map<BillingState, BillingChangedEto>(billingState));

        var list = await _billingService.GetListAsync(AeFinderApplicationTestConsts.OrganizationId,
            new GetBillingInput());
        list.Items.Count.ShouldBe(2);

        list = await _billingService.GetListAsync(AeFinderApplicationTestConsts.OrganizationId,
            new GetBillingInput
            {
                Type = BillingType.Settlement
            });
        list.Items.Count.ShouldBe(1);
        
        list = await _billingService.GetListAsync(AeFinderApplicationTestConsts.OrganizationId,
            new GetBillingInput
            {
                Status = BillingStatus.Unpaid
            });
        list.Items.Count.ShouldBe(1);

        var billing = await _billingService.GetAsync(AeFinderApplicationTestConsts.OrganizationId, billingState.Id);
        billing.Type.ShouldBe(BillingType.AdvancePayment);
    }
}