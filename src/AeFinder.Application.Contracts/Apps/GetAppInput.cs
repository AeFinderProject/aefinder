using System;
using Volo.Abp.Application.Dtos;

namespace AeFinder.Apps;

public class GetAppInput : PagedResultRequestDto
{
    public Guid? OrganizationId { get; set; }
    public string AppId { get; set; }
}