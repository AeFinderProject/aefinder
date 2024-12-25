using System;
using Volo.Abp.Application.Dtos;

namespace AeFinder.Orders;

public class GetOrderListInput: PagedResultRequestDto
{
    public DateTime BeginTime { get; set; }
    public DateTime EndTime { get; set; }
}