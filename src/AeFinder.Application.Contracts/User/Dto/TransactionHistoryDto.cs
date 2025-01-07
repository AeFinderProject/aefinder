using System;

namespace AeFinder.User.Dto;

public class TransactionHistoryDto
{
    public string TransactionId { get; set; }
    public string TransactionDescription { get; set; }
    public decimal TransactionAmount { get; set; }
    public DateTime TransactionDate { get; set; }
    public decimal BalanceAfter { get; set; }
    public decimal LockedBalance { get; set; }
    public string PaymentMethod { get; set; }
}