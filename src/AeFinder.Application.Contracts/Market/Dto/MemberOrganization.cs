using System.Collections.Generic;

namespace AeFinder.Market;

public class MemberOrganization
{
    public long TotalCount { get; set; }
    public List<MemberOrganizationDto> Items { get; set; }
}