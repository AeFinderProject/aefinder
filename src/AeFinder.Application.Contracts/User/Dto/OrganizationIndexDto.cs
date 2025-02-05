using System.Collections.Generic;
using AeFinder.Enums;

namespace AeFinder.User.Dto;

public class OrganizationIndexDto
{
    public string OrganizationId { get; set; }
    public string OrganizationName { get; set; }
    public int MaxAppCount { get; set; }
    public List<string> AppIds { get; set; }
    public OrganizationStatus Status { get; set; }
}