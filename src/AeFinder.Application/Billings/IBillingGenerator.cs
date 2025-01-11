using System;
using System.Threading.Tasks;
using AeFinder.Grains.State.Billings;

namespace AeFinder.Billings;

public interface IBillingGenerator
{
    BillingType BillingType { get; }

    Task<BillingState> GenerateBillingAsync(Guid organizationId, DateTime dateTime);
}