using AElfIndexer.CodeOps.Patchers.Module;
using Mono.Cecil.Rocks;
using Xunit;

namespace AElfIndexer.CodeOps.Tests.Patcher.Module;

public class EntityIndexingPatcherTests:AElfIndexerCodeOpsTestBase
{
    private readonly EntityIndexingPatcher _entityIndexingPatcher;

    public EntityIndexingPatcherTests()
    {
        _entityIndexingPatcher = GetRequiredService<EntityIndexingPatcher>();
    }

    [Fact]
    public void PatchTest()
    {
        var sourceCode = @"
        using System.Collections.Generic;
        using AElfIndexer.Sdk;

        namespace MyPlugin;

        public class MyEntityBase : IndexerEntity
        {
            public int BaseIntValue { get; set; }
            public string BaseStringValue { get; set; }
            [Fulltext(Index = true)]
            public string BaseTextString { get; set; }
        }

        public class MyEntity : MyEntityBase, IIndexerEntity
        {
            public int IntValue { get; set; }
            public string StringValue { get; set; }
            public Detail Detail { get; set; }
            public List<string> ListString { get; set; }
            public List<ListDetail> Details { get; set; }

            [Fulltext(Index = true)]
            public string TextString { get; set; }
            [Fulltext]
            public string TextString2 { get; set; }
            
            private string _privateString;

            public string OnlyGet { get; }
        }

        public class Detail
        {
            public int DetailIntValue { get; set; }
            public string DetailStringValue { get; set; }
            [Fulltext(Index = true)]
            public string DetailStringValue2 { get; set; }
        }

        public class ListDetail
        {
            public int DetailIntValue { get; set; }
            public string DetailStringValue { get; set; }
            [Fulltext(Index = true)]
            public string DetailStringValue2 { get; set; }
        }
        ";
        var asm = CompileToAssemblyDefinition(sourceCode);
        var module = asm.MainModule;
        
        _entityIndexingPatcher.Patch(module);

        var expectedCode = @"
        using System.Collections.Generic;
        using AElfIndexer.Sdk;
        using Nest;

        namespace MyPlugin;

        public class MyEntity : MyEntityBase,IIndexerEntity
        {
            private string _privateString;
            public int IntValue { get; set; }
            [Keyword]
            public string StringValue { get; set; }
            public Detail Detail { get; set; }
            public List<string> ListString { get; set; }
            public List<ListDetail> Details { get; set; }
            [Text(Index = true)]
            public string TextString { get; set; }
            [Text(Index = false)]
            public string TextString2 { get; set; }
            [Keyword]
            public string OnlyGet { get; }
        }
        ";
        
        var patchedCode = DecompileType(module.GetAllTypes().Single(t => t.FullName == "MyPlugin.MyEntity"));
        Assert.Equal(FormatCode(expectedCode), FormatCode(patchedCode));
        
        var expectedBaseCode = @"
        using AElfIndexer.Sdk;        
        using Nest;

        namespace MyPlugin;

        public class MyEntityBase : IndexerEntity
        {
            public int BaseIntValue { get; set; }
            [Keyword]
            public string BaseStringValue { get; set; }
            [Text(Index = true)]
            public string BaseTextString { get; set; }
        }
        ";
        
        patchedCode = DecompileType(module.GetAllTypes().Single(t => t.FullName == "MyPlugin.MyEntityBase"));
        Assert.Equal(FormatCode(expectedBaseCode), FormatCode(patchedCode));
        
        var expectedDetailCode = @"
        using Nest;

        namespace MyPlugin;

        public class Detail
        {
            public int DetailIntValue { get; set; }
            [Keyword]
            public string DetailStringValue { get; set; }
            [Text(Index = true)]
            public string DetailStringValue2 { get; set; }
        }";
        
        patchedCode = DecompileType(module.GetAllTypes().Single(t => t.FullName == "MyPlugin.Detail"));
        Assert.Equal(FormatCode(expectedDetailCode), FormatCode(patchedCode));
        
        var expectedListDetailCode = @"
        using Nest;

        namespace MyPlugin;

        public class ListDetail
        {
            public int DetailIntValue { get; set; }
            [Keyword]
            public string DetailStringValue { get; set; }
            [Text(Index = true)]
            public string DetailStringValue2 { get; set; }
        }";
        
        patchedCode = DecompileType(module.GetAllTypes().Single(t => t.FullName == "MyPlugin.ListDetail"));
        Assert.Equal(FormatCode(expectedListDetailCode), FormatCode(patchedCode));
    }
}