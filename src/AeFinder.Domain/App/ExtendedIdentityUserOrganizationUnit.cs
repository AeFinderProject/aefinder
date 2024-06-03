using System;
using Volo.Abp.Identity;

namespace AeFinder.App;

public class ExtendedIdentityUserOrganizationUnit: IdentityUserOrganizationUnit
{
    public ExtendedIdentityUserOrganizationUnit(Guid userId, Guid organizationUnitId, Guid? tenantId = null)
    {
        UserId = userId;
        OrganizationUnitId = organizationUnitId;
        TenantId = tenantId;
        Id = Guid.NewGuid();
    }
    
    public Guid Id { get; set; }
}