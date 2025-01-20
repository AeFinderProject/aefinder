using System;
using AeFinder.Enums;

namespace AeFinder.User.Dto;

public class OrganizationExtensionDto
{
    public Guid OrganizationId { get; set; }
    
    public string OrganizationWalletAddress { get; set; }
    public OrganizationStatus OrganizationStatus { get; set; }
}