using System.Threading.Tasks;
using AeFinder.BlockChainEventHandler.DTOs;

namespace AeFinder.BlockChainEventHandler.Processors;

public interface IBlockChainDataEventProcessor
{
    Task HandleEventAsync(BlockChainDataEto eventData);
}