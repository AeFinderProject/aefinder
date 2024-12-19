using Volo.Abp.Application.Dtos;

namespace AeFinder.Market;

public class IndexerUserBalanceDto
{
    // public  UserBalance UserBalance { get; set; }
    public PagedResultDto<UserBalanceDto> UserBalance { get; set; }
}