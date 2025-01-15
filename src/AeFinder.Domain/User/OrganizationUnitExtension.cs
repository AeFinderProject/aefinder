using System;
using AeFinder.Enums;
using Volo.Abp.Domain.Entities;

namespace AeFinder.User;

public class OrganizationUnitExtension: Entity<Guid>
{
    public Guid OrganizationId { get; set; }
    public string OrganizationWalletAddress { get; set; }
    public OrganizationStatus OrganizationStatus { get; set; }
    public OrganizationUnitExtension(Guid id)
    {
        Id = id;
    }
}