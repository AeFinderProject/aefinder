using AeFinder.Sdk.Dtos;

namespace TokenApp.GraphQL;

public class TransferRecordDto : AeFinderEntityDto
{
    public string Symbol { get; set; }
    public string FromAddress { get; set; }
    public string ToAddress { get; set; }
    public long Amount { get; set; }
}