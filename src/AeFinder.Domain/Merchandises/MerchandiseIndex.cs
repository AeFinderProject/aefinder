using System;
using AElf.EntityMapping.Entities;
using Nest;

namespace AeFinder.Merchandises;

public class MerchandiseIndex: AeFinderDomainEntity<Guid>, IEntityMappingEntity
{
    [Keyword]
    public string Name { get; set; }
    [Keyword]
    public string Description  { get; set; }
    [Keyword]
    public string Unit { get; set; }
    public decimal Price { get; set; }
    public int ChargeType { get; set; }
    public int Category { get; set; }
    public int Type { get; set; }
    public int Status  { get; set; }
    public int SortWeight { get; set; }
}