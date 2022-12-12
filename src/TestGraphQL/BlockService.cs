using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client.Providers;
using Volo.Abp.Identity;
using Volo.Abp.Uow;

namespace GraphQL;

public class BlockService : IBlockService
{
    private readonly INESTRepository<TestBlockIndex, string> _nestRepository;
    private readonly string _indexName;

    public BlockService(INESTRepository<TestBlockIndex, string> nestRepository,IAElfIndexerClientInfoProvider<Query> provider)
    {
        _nestRepository = nestRepository;
        _indexName = $"{provider.GetClientId()}{provider.GetVersion()}.{nameof(TestBlockIndex)}".ToLower();
    }

    public async Task<TestBlock> GetBlockAsync(string id)
    {
        var block =  await _nestRepository.GetAsync(id, _indexName);
        return block !=null? new TestBlock
        {
            Id = block.Id,
            BlockHash = block.BlockHash,
            BlockHeight = block.BlockHeight,
            ChainId = block.ChainId
        }:new TestBlock
        {
            BlockHash = "abc"
        };
    }
}