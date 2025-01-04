using System;
using Volo.Abp.Application.Dtos;

namespace AeFinder.Billings;

public class GetBillingInput: PagedResultRequestDto
{
    public DateTime? BeginTime { get; set; }
    public DateTime? EndTime { get; set; }
    public BillingType? Type { get; set; }
    public BillingStatus? Status { get; set; }
    public BillingSortType Sort { get; set; } = BillingSortType.BillingTimeDesc;
}

public enum BillingSortType
{
    BillingTimeAsc,
    BillingTimeDesc
}