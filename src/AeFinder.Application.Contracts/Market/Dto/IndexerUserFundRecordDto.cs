using Volo.Abp.Application.Dtos;

namespace AeFinder.Market;

public class IndexerUserFundRecordDto
{
    public PagedResultDto<UserFundRecordDto> UserFundRecord { get; set; }
}