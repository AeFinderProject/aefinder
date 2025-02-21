using System;
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
    private readonly IBillingManagementService _billingManagementService;

    public BillingPaymentController(IBillingPaymentService billingPaymentService, IBillingManagementService billingManagementService)
    {
        _billingPaymentService = billingPaymentService;
        _billingManagementService = billingManagementService;
    }
    
    [HttpPost]
    [Route("repay")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task RepayFailedBillingAsync(RepayFailedBillingInput input)
    {
        await _billingManagementService.RePayAsync(Guid.Parse(input.BillingId));
    }
    
    [HttpGet]
    [Route("treasurer")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task<string> GetAsync()
    {
        return await _billingPaymentService.GetTreasurerAsync();
    }
}