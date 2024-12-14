using System.Collections.Generic;

namespace AeFinder.Market;

public class UserBalance
{
    public long TotalCount { get; set; }
    public List<UserBalanceDto> Items { get; set; }
}