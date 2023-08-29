using System;
using System.Collections.Generic;
using AElf.EntityMapping.Entities;
using AElfIndexer.Entities;
using Nest;

namespace AElfIndexer.Entities.Es;

public class BlockIndex:BlockBase,IEntityMappingEntity
{
    // public List<Transaction> Transactions {get;set;}
    public List<string> TransactionIds { get; set; }
    public int LogEventCount { get; set; }
}