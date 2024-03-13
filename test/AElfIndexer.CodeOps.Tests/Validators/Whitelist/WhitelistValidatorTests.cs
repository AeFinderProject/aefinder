using AElfIndexer.CodeOps.Validators.Whitelist;
using GraphQL;
using Microsoft.Extensions.Logging;
using Nest;
using Shouldly;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace AElfIndexer.CodeOps.Tests.Validators.Whitelist;

public class WhitelistValidatorTests : AElfIndexerCodeOpsTestBase
{
    private readonly WhitelistValidator _whitelistValidator;

    public WhitelistValidatorTests()
    {
        _whitelistValidator = GetRequiredService<WhitelistValidator>();
    }

    [Fact]
    public void ValidateTest()
    {
        var sourceCode = @"
        using AElfIndexer.Sdk;
        using GraphQL;
        using Volo.Abp.Modularity;
        using Volo.Abp.ObjectMapping;
        using System;
        using System.Threading.Tasks;
        using System.Linq;
        using System.Collections.Generic;
        using Nest;

        namespace Plugin;

        public class PluginEntity : IndexerEntity, IIndexerEntity
        {
            public int IntValue { get; set; }
            [Keyword]
            public string StringValue { get; set; }
            [Text]
            public string StringValue2 { get; set; }
        }
        
        public class PluginEntityDto
        {
            
        }

        public class Query
        {
            public static async Task<List<PluginEntityDto>> TokenInfo(
                [FromServices] IReadOnlyRepository<PluginEntity> repository,
                [FromServices] IObjectMapper objectMapper, string chainId)
            {
                var query = await repository.GetQueryableAsync();
                var list = query.ToList();

                return objectMapper.Map<List<PluginEntity>, List<PluginEntityDto>>(list);
            }
        }

        public class PluginAppSchema : AppSchema<Query>
        {
            protected PluginAppSchema(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
        }

        public class PluginBlockProcessor : BlockProcessorBase
        {
            public override async Task ProcessAsync(Block block)
            {
                throw new NotImplementedException();
            }
        }

        public class PluginModule:AbpModule
        {
            public override void ConfigureServices(ServiceConfigurationContext context)
            {
            }
        }
        ";
        AddAssemblies(typeof(FromServicesAttribute).Assembly.Location, typeof(IObjectMapper).Assembly.Location,
            typeof(AbpModule).Assembly.Location, typeof(KeywordAttribute).Assembly.Location);
        var assemblyDefinition = CompileToAssemblyDefinition(sourceCode);

        var validationResult = _whitelistValidator.Validate(assemblyDefinition.MainModule, CancellationToken.None);
        validationResult.Count().ShouldBe(0);
    }

    [Fact]
    public void Validate_Failed_Test()
    {
        var sourceCode = @"
        using AElfIndexer.Sdk;
        using Nest;
        using System.Net.Http;
        using Microsoft.Extensions.Logging;
        using System.Threading.Tasks;
        using System;

        namespace Plugin;

        public class PluginEntity : IndexerEntity, IIndexerEntity
        {
            public int IntValue { get; set; }
            [Keyword]
            public string StringValue { get; set; }
        }

        public class Client
        {
            public HttpClient GetClient()
            {
                return new HttpClient();
            }
        }

        public class PluginBlockProcessor : BlockProcessorBase
        {
            private readonly ILogger<PluginBlockProcessor> _logger;

            public override async Task ProcessAsync(Block block)
            {
                throw new NotImplementedException();
            }
        }
        ";
        AddAssemblies(typeof(FromServicesAttribute).Assembly.Location, typeof(IObjectMapper).Assembly.Location,
            typeof(AbpModule).Assembly.Location, typeof(KeywordAttribute).Assembly.Location, typeof(ILogger).Assembly.Location);
        var assemblyDefinition = CompileToAssemblyDefinition(sourceCode);

        var validationResults = _whitelistValidator.Validate(assemblyDefinition.MainModule, CancellationToken.None)
            .ToList();
        validationResults.Count.ShouldBeGreaterThan(0);
        validationResults.ShouldContain(v => v.Message == "Assembly System.Net.Http is not allowed.");
        validationResults.ShouldContain(v => v.Message == "Assembly Microsoft.Extensions.Logging.Abstractions is not allowed.");
        validationResults.ShouldContain(v => v.Message == "System.Net.Http is not allowed.");
    }
}