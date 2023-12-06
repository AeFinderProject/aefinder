using AElfIndexer.CodeOps.Validators.Assembly;
using Shouldly;
using Xunit;

namespace AElfIndexer.CodeOps.Tests.Validators.Assembly;

public class IndexerEntityValidatorTests : AElfIndexerCodeOpsTestBase
{
    private readonly IndexerEntityValidator _indexerEntityValidator;

    public IndexerEntityValidatorTests()
    {
        _indexerEntityValidator = GetRequiredService<IndexerEntityValidator>();
    }

    [Fact]
    public void ValidateTest()
    {
        var sourceCode = @"
        using AElfIndexer.Sdk;

        namespace MyPlugin;

        public class MyEntity1 : IndexerEntity, IIndexerEntity
        {
            public int IntValue { get; set; }
            public string StringValue { get; set; }

            public MyEntity1(string id) : base(id)
            {
            }
        }

        public class MyEntity2 : IndexerEntity, IIndexerEntity
        {
            public int IntValue { get; set; }
            public string StringValue { get; set; }

            public MyEntity2(string id) : base(id)
            {
            }
        }

        public class MyEntity3 : IndexerEntity
        {
            public int IntValue { get; set; }
            public string StringValue { get; set; }

            public MyEntity3(string id) : base(id)
            {
            }
        }

        public class MyEntity4 : IIndexerEntity
        {
            public int IntValue { get; set; }
            public string StringValue { get; set; }
        }

        public class MyEntity5 : MyEntity3, IIndexerEntity
        {
            public int IntValue2 { get; set; }
            public string StringValue2 { get; set; }

            public MyEntity5(string id) : base(id)
            {
            }
        }
        ";
        
        var assemblyDefinition = CompileToAssemblyDefinition(sourceCode);
        var codeStream = new MemoryStream();
        assemblyDefinition.Write(codeStream);
        var assembly = System.Reflection.Assembly.Load(codeStream.ToArray());
        
        var validationResult = _indexerEntityValidator.Validate(assembly, CancellationToken.None);
        validationResult.Count().ShouldBe(0);
    }
    
    [Fact]
    public void Validate_Exceeds_Limit_Test()
    {
        var sourceCode = @"
        using AElfIndexer.Sdk;

        namespace MyPlugin;

        public class MyEntity1 : IndexerEntity, IIndexerEntity
        {
            public int IntValue { get; set; }
            public string StringValue { get; set; }

            public MyEntity1(string id) : base(id)
            {
            }
        }

        public class MyEntity2 : IndexerEntity, IIndexerEntity
        {
            public int IntValue { get; set; }
            public string StringValue { get; set; }

            public MyEntity2(string id) : base(id)
            {
            }
        }

        public class MyEntity3 : IndexerEntity, IIndexerEntity
        {
            public int IntValue2 { get; set; }
            public string StringValue2 { get; set; }

            public MyEntity3(string id) : base(id)
            {
            }
        }

        public class MyEntity4 : IndexerEntity, IIndexerEntity
        {
            public int IntValue { get; set; }
            public string StringValue { get; set; }

            public MyEntity4(string id) : base(id)
            {
            }
        }

        public class MyEntity5 : MyEntity4
        {
            public int IntValue2 { get; set; }
            public string StringValue2 { get; set; }

            public MyEntity5(string id) : base(id)
            {
            }
        }
        ";
        
        var assemblyDefinition = CompileToAssemblyDefinition(sourceCode);
        var codeStream = new MemoryStream();
        assemblyDefinition.Write(codeStream);
        var assembly = System.Reflection.Assembly.Load(codeStream.ToArray());
        
        var validationResult = _indexerEntityValidator.Validate(assembly, CancellationToken.None).ToList();
        validationResult.Count().ShouldBe(1);
        validationResult[0].Message.ShouldBe("Entity count 4 exceeds the limit 3.");
    }
}