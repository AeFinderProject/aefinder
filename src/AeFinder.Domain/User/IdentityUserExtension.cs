using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities;

namespace AeFinder.User;

public class IdentityUserExtension: Entity<Guid>
{
    public Guid UserId { get; set; }
    public string AElfAddress { get; set; }
    public string CaHash { get; set; }
    public string CaAddressMain { get; set; }
    public List<UserChainAddressInfo> CaAddressList { get; set; }
    
    public IdentityUserExtension(Guid id)
    {
        Id = id;
    }
}