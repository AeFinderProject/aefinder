using Volo.Abp.Application.Dtos;

namespace AeFinder.ApiKeys;

public class GetApiKeyQueryApiInput: PagedResultRequestDto
{
    public BasicApi? Api { get; set; }
}