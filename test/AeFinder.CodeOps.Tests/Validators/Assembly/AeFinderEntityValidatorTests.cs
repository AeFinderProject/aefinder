using Shouldly;
using Xunit;

namespace AeFinder.CodeOps.Validators.Assembly;

public class AeFinderEntityValidatorTests : AeFinderCodeOpsTestBase
{
    private readonly AeFinderEntityValidator _aeFinderEntityValidator;

    public AeFinderEntityValidatorTests()
    {
        _aeFinderEntityValidator = GetRequiredService<AeFinderEntityValidator>();
    }

    [Fact]
    public void ValidateTest()
    {
        var sourceCode = @"
        using AeFinder.Sdk;
        using AeFinder.Sdk.Entities;

        namespace MyPlugin;

        public class MyEntity1 : AeFinderEntity, IAeFinderEntity
        {
            public int IntValue { get; set; }
            public string StringValue { get; set; }
        }

        public class MyEntity2 : AeFinderEntity, IAeFinderEntity
        {
            public int IntValue { get; set; }
            public string StringValue { get; set; }
        }

        public class MyEntity3 : AeFinderEntity
        {
            public int IntValue { get; set; }
            public string StringValue { get; set; }
        }

        public class MyEntity4 : IAeFinderEntity
        {
            public int IntValue { get; set; }
            public string StringValue { get; set; }
        }

        public class MyEntity5 : MyEntity3, IAeFinderEntity
        {
            public int IntValue2 { get; set; }
            public string StringValue2 { get; set; }
        }
        ";
        
        var assemblyDefinition = CompileToAssemblyDefinition(sourceCode);
        var codeStream = new MemoryStream();
        assemblyDefinition.Write(codeStream);
        var assembly = System.Reflection.Assembly.Load(codeStream.ToArray());
        
        var validationResult = _aeFinderEntityValidator.Validate(assembly, CancellationToken.None);
        validationResult.Count().ShouldBe(0);
    }
    
    [Fact]
    public void Validate_Exceeds_Limit_Test()
    {
        var sourceCode = @"
        using AeFinder.Sdk;
        using AeFinder.Sdk.Entities;

        namespace MyPlugin;

        public class MyEntity1 : AeFinderEntity, IAeFinderEntity
        {
            public int IntValue { get; set; }
            public string StringValue { get; set; }
        }

        public class MyEntity2 : AeFinderEntity, IAeFinderEntity
        {
            public int IntValue { get; set; }
            public string StringValue { get; set; }
        }

        public class MyEntity3 : AeFinderEntity, IAeFinderEntity
        {
            public int IntValue2 { get; set; }
            public string StringValue2 { get; set; }
        }

        public class MyEntity4 : AeFinderEntity, IAeFinderEntity
        {
            public int IntValue { get; set; }
            public string StringValue { get; set; }
        }

        public class MyEntity5 : MyEntity4
        {
            public int IntValue2 { get; set; }
            public string StringValue2 { get; set; }
        }
        ";
        
        var assemblyDefinition = CompileToAssemblyDefinition(sourceCode);
        var codeStream = new MemoryStream();
        assemblyDefinition.Write(codeStream);
        var assembly = System.Reflection.Assembly.Load(codeStream.ToArray());
        
        var validationResult = _aeFinderEntityValidator.Validate(assembly, CancellationToken.None).ToList();
        validationResult.Count().ShouldBe(1);
        validationResult[0].Message.ShouldBe("Entity count 4 exceeds the limit 3.");
    }
}