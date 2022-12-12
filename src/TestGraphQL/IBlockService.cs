namespace GraphQL;

public interface IBlockService
{
    Task<TestBlock> GetBlockAsync(string id);
}