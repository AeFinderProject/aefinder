using System;

namespace AeFinder.User.Dto;

public class OrganizationExtensionDto
{
    public Guid OrganizationId { get; set; }
    
    public string OrganizationWalletAddress { get; set; }
}