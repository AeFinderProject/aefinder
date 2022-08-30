using System.Threading.Tasks;
using AElfScan.AElf.Dtos;

namespace AElfScan.AElf;

public interface IAElfAppService
{
    Task SaveBlock(BlockEventDataDto eventData);
}