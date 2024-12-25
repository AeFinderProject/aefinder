using System;
using Volo.Abp.Application.Dtos;

namespace AeFinder.Billings;

public class GetBillingInput: PagedResultRequestDto
{
    public DateTime DateTime { get; set; }
    public BillingType Type { get; set; }
    public GetBillingSortType SortType { get; set; }
}

public enum GetBillingSortType
{
    
}