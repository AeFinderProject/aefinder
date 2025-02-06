using System;
using AeFinder.Enums;

namespace AeFinder.Organizations;

public class OrganizationUnitExtensionUpdateEto
{
    public Guid OrganizationId { get; set; }
    public OrganizationStatus OrganizationStatus { get; set; }
}