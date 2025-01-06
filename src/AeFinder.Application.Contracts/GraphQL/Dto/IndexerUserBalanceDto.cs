using Volo.Abp.Application.Dtos;

namespace AeFinder.GraphQL.Dto;

public class IndexerUserBalanceDto
{
    public PagedResultDto<UserBalanceDto> UserBalance { get; set; }
}