using System;
using System.Threading.Tasks;

namespace AeFinder.Orders;

public interface IOrderService
{
    Task ConfirmPaymentAsync(Guid orderId, string transactionId, DateTime paymentTime);
}