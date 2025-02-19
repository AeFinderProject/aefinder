using System;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Assets;
using AeFinder.Billings;
using AeFinder.Merchandises;
using AeFinder.Models;
using AeFinder.User;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Timing;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("BillingManagement")]
[Route("api/billings/management")]
public class BillingManagementController : AeFinderController
{
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IBillingManagementService _billingManagementService;

    public BillingManagementController(IOrganizationAppService organizationAppService,
        IBillingManagementService billingManagementService)
    {
        _organizationAppService = organizationAppService;
        _billingManagementService = billingManagementService;
    }
    
    [HttpPost]
    [Route("monthly-billing")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task GenerateMonthlyBillingAsync(GenerateMonthlyBillingInput input)
    {
        await _billingManagementService.GenerateMonthlyBillingAsync(input.OrganizationId, input.DateTime);
    }
}