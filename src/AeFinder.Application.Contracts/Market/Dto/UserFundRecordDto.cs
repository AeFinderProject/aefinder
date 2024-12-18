using AeFinder.Sdk.Dtos;

namespace AeFinder.Market;

public class UserFundRecordDto: AeFinderEntityDto
{
    public string Address { get; set; }
    
    public string TransactionId { get; set; }
    
    public decimal Amount { get; set; }
    
    public TokenInfoDto Token { get; set; }
    
    public decimal Balance { get; set; }
    
    public decimal LockedBalance { get; set; }
    
    public UserFundRecordType Type { get; set; }
    
    public string BillingId { get; set; }
}