using Volo.Abp.Application.Dtos;

namespace AeFinder.Apps;

public class GetAppPodUsageDurationInput: PagedResultRequestDto
{
    public string AppId { get; set; }
    public string Version { get; set; }
}