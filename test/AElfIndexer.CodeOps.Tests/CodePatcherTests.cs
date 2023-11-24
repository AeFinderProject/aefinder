using Xunit;

namespace AElfIndexer.CodeOps.Tests;

public class CodePatcherTests : AElfIndexerCodeOpsTestBase
{
    private readonly ICodePatcher _codePatcher;

    public CodePatcherTests()
    {
        _codePatcher = GetRequiredService<ICodePatcher>();
    }

    [Fact]
    public void Test()
    {
        var originCode =
            File.ReadAllBytes("/Users/zx/code/tmp/aelf-indexer/test/TestEntity/bin/Debug/net7.0/TestEntity.dll");
        var code = _codePatcher.Patch(originCode);
        Assert.NotNull(code);
    }
}