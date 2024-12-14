using System.Collections.Generic;

namespace AeFinder.Market;

public class MemberOrganizationDto
{
    public string Address { get; set; }
    
    public List<OrganizationMemberDto> Members { get; set; }
}