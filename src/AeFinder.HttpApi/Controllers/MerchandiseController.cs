using System;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.ApiKeys;
using AeFinder.Grains;
using AeFinder.Merchandises;
using AeFinder.User;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Timing;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("Merchandise")]
[Route("api/merchandises")]
public class MerchandiseController : AeFinderController
{
    private readonly IMerchandiseService _merchandiseService;

    public MerchandiseController(IMerchandiseService merchandiseService)
    {
        _merchandiseService = merchandiseService;
    }

    [HttpGet]
    [Authorize]
    public async Task<ListResultDto<MerchandiseDto>> GetListedMerchandisesAsync(GetMerchandiseInput input)
    {
        return await _merchandiseService.GetListAsync(input);
    }
    
    [HttpGet]
    [Route("all")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task<ListResultDto<MerchandiseDto>> GetAllMerchandisesAsync(GetMerchandiseInput input)
    {
        return await _merchandiseService.GetAllListAsync(input);
    }

    [HttpPost]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task<MerchandiseDto> CreateMerchandiseAsync(CreateMerchandiseInput input)
    {
        return await _merchandiseService.CreateAsync(input);
    }

    [HttpPut]
    [Route("{id}")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task<MerchandiseDto> UpdateMerchandiseAsync(Guid id, UpdateMerchandiseInput input)
    {
        return await _merchandiseService.UpdateAsync(id, input);
    }
}