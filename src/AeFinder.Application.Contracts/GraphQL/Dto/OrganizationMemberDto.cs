using AeFinder.Enums;

namespace AeFinder.GraphQL.Dto;

public class OrganizationMemberDto
{
    public string Address { get; set; }
    
    public OrganizationMemberRole Role { get; set; }
}