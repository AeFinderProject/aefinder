using Volo.Abp.Application.Dtos;

namespace AeFinder.Apps;

public class GetAppInput : PagedResultRequestDto
{
    public string OrganizationId { get; set; }
    public string AppId { get; set; }
}