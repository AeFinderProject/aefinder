using System;

namespace AeFinder.Models;

public class GenerateMonthlyBillingInput
{
    public Guid OrganizationId { get; set; }
    public DateTime DateTime { get; set; }
}