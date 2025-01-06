using System.Collections.Generic;

namespace AeFinder.GraphQL.Dto;

public class MemberOrganizationDto
{
    public string Address { get; set; }
    
    public List<OrganizationMemberDto> Members { get; set; }
}