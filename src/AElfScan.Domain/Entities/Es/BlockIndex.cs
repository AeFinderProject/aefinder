using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using AElfScan.Entities;
using Nest;

namespace AElfScan.Entities.Es;

public class BlockIndex:BlockBase,IIndexBuild
{
    // [Nested(Name = "Transactions",Enabled = true,IncludeInParent = true,IncludeInRoot = true)]
    // public List<Transaction> Transactions {get;set;}
}