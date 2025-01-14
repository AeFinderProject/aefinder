using System.Threading.Tasks;
using AeFinder.Billings;
using AeFinder.Billings.Dto;
using AeFinder.User;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("BillingPayment")]
[Route("api/billing/payment")]
public class BillingPaymentController : AeFinderController
{
    private readonly IBillingPaymentService _billingPaymentService;

    public BillingPaymentController(IBillingPaymentService billingPaymentService)
    {
        _billingPaymentService = billingPaymentService;
    }
    
    [HttpPost]
    [Route("{id}/index")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task RepayFailedBillingAsync(RepayFailedBillingInput input)
    {
        await _billingPaymentService.RepayFailedBillingAsync(input.OrganizationId, input.BillingId);
    }
    
    [HttpGet]
    [Route("treasurer")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task<string> GetAsync()
    {
        return await _billingPaymentService.GetTreasurerAsync();
    }
}