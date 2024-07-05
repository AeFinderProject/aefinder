using System.Collections.Generic;
using Orleans;

namespace AeFinder.Block.Dtos;

[GenerateSerializer]
public class BlockWithTransactionDto : BlockDto
{
    [Id(0)] public List<TransactionDto> Transactions { get; set; } = new();
}