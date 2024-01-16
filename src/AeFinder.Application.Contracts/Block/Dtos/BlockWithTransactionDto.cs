using System.Collections.Generic;

namespace AeFinder.Block.Dtos;

public class BlockWithTransactionDto : BlockDto
{
    public List<TransactionDto> Transactions { get; set; } = new();
}