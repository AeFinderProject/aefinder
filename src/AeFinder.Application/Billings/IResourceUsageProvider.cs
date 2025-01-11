using System;
using System.Threading.Tasks;
using AeFinder.Merchandises;

namespace AeFinder.Billings;

public interface IResourceUsageProvider
{
    MerchandiseType MerchandiseType { get; }
    Task<long> GetUsageAsync(Guid organizationId, DateTime dateTime);
}