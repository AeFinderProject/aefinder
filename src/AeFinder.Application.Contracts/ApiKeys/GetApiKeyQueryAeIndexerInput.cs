using Volo.Abp.Application.Dtos;

namespace AeFinder.ApiKeys;

public class GetApiKeyQueryAeIndexerInput: PagedResultRequestDto
{
    public string AppId { get; set; }
}