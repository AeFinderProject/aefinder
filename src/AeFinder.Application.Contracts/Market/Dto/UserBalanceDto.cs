using AeFinder.Sdk.Dtos;

namespace AeFinder.Market;

public class UserBalanceDto: AeFinderEntityDto
{
    public string Address { get; set; }
    public string Symbol { get; set; }
    public decimal Balance { get; set; }
    public decimal LockedBalance { get; set; }
    public TokenInfoDto Token { get; set; }
}