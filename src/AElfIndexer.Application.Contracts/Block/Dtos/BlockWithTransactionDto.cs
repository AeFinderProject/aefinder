using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace AElfIndexer.Block.Dtos;

public class BlockWithTransactionDto : BlockDto
{
    public List<TransactionDto> Transactions { get; set; } = new();
}