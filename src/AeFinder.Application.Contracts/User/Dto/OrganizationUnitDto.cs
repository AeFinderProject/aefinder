using System;
using AeFinder.Enums;

namespace AeFinder.User.Dto;

public class OrganizationUnitDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; }
    public DateTime CreationTime { get; set; }
    public string OrganizationWalletAddress { get; set; }
    public OrganizationStatus OrganizationStatus { get; set; }
}