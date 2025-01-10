using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Orders;
using Volo.Abp.Application.Dtos;

namespace AeFinder.Assets;

public interface IAssetService
{
    Task AddOrUpdateIndexAsync(AssetChangedEto input);
    Task UpdateIndexAsync(Guid id);
    Task<AssetDto> CreateAsync(Guid organizationId, CreateAssetInput input);
    Task<List<Guid>> ChangeAssetAsync(OrderStatusChangedEto input);
    Task<PagedResultDto<AssetDto>> GetListAsync(Guid organizationId, GetAssetInput input);
    Task StartUsingAssetAsync(Guid id, DateTime dateTime);
    Task ReleaseAssetAsync(Guid id, DateTime dateTime);
    Task PayAsync(Guid id, decimal paidAmount);
    Task RelateAppAsync(Guid organizationId, RelateAppInput input);
    Task<decimal> CalculateMonthlyCostAsync(Guid organizationId, DateTime dateTime);
    Task LockAsync(Guid id, bool isLock);
}