using System;
using Volo.Abp.Domain.Entities;

namespace AeFinder.User;

public class OrganizationUnitExtension: Entity<Guid>
{
    public Guid OrganizationId { get; set; }
    public string OrganizationWalletAddress { get; set; }
    public OrganizationUnitExtension(Guid id)
    {
        Id = id;
    }
}