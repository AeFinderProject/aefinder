using AElfScan.AElf.DTOs;
using AElfScan.AElf.Etos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AElfScan.AElf.Mappers;

public class BlockMapper: IObjectMapper<BlockEto, NewBlockEto>, ITransientDependency
{
    private readonly IAutoObjectMappingProvider _mapperProvider;

    public BlockMapper(IAutoObjectMappingProvider mapperProvider)
    {
        _mapperProvider = mapperProvider;
    }

    public NewBlockEto Map(BlockEto source)
    {
        var newBlock = _mapperProvider.Map<BlockEto, NewBlockEto>(source);
        return newBlock;
    }

    public NewBlockEto Map(BlockEto source, NewBlockEto destination)
    {
        throw new System.NotImplementedException();
    }
}