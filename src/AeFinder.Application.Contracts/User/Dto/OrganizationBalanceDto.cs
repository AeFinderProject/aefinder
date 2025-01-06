using AeFinder.GraphQL.Dto;

namespace AeFinder.User.Dto;

public class OrganizationBalanceDto
{
    public string ChainId { get; set; }
    public string Address { get; set; }
    public string Symbol { get; set; }
    public decimal Balance { get; set; }
    public decimal LockedBalance { get; set; }
    public TokenInfoDto Token { get; set; }
}