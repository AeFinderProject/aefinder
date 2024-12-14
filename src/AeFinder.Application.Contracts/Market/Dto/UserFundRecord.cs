using System.Collections.Generic;

namespace AeFinder.Market;

public class UserFundRecord
{
    public long TotalCount { get; set; }
    public List<UserFundRecordDto> Items { get; set; }
}