using System;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Etos;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;

namespace AeFinder.EntityEventHandler;

public partial class BlockHandler
{
    public virtual async Task<FlowBehavior> HandleNewBlockExceptionAsync(Exception exception, NewBlocksEto eventData)
    {
        // Record a log. And retry to ensure that the message is not lost.
        _logger.LogError(exception,
            "Handle new blocks error. start BlockHeight: {StartBlockHeight}, end BlockHeight: {EndBlockHeight},retrying...",
            eventData.NewBlocks.First().BlockHeight, eventData.NewBlocks.Last().BlockHeight);
        await HandleEventAsync(eventData);

        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
        };
    }
}