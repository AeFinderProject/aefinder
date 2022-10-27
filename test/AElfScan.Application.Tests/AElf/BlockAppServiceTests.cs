using AElf.Indexing.Elasticsearch;
using AElfScan.AElf.Entities.Es;
using AElfScan.AElf.Etos;
using Volo.Abp.EventBus.Distributed;

namespace AElfScan.AElf;

public class BlockAppServiceTests:AElfScanApplicationTestBase
{
    private readonly INESTRepository<Block, string> _blockIndexRepository;

    public BlockAppServiceTests()
    {
        _blockIndexRepository = GetRequiredService<INESTRepository<Block, string>>();
    }
}