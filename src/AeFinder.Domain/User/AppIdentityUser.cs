using System.Collections.Generic;
using Volo.Abp.Identity;

namespace AeFinder.User;

public class AppIdentityUser: IdentityUser
{
    public string AElfAddress { get; set; }
    public string CaHash { get; set; }
    public string CaAddressMain { get; set; }
    public List<UserChainAddress> CaAddressListSide { get; set; }
}