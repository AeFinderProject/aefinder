using Volo.Abp.Application.Dtos;

namespace AeFinder.AppResources.Dto;

public class GetAppResourceUsageInput: PagedResultRequestDto
{
    public string AppId { get; set; }
}