using System.Text.RegularExpressions;
using AElf.EntityMapping;
using AElf.EntityMapping.Elasticsearch;
using GraphQL;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace AeFinder.CodeOps.Validators.Whitelist;

public class WhitelistValidatorTests : AeFinderCodeOpsTestBase
{
    private readonly WhitelistValidator _whitelistValidator;

    public WhitelistValidatorTests()
    {
        _whitelistValidator = GetRequiredService<WhitelistValidator>();
    }
    
    public static bool Match(string source, string pattern)
    {
        return source.IsNullOrEmpty() ? false : Regex.IsMatch(source, pattern);
    }

    [Fact]
    public void ValidateTest()
    {
        var sourceCode = @"
        using AeFinder.Sdk;
        using AeFinder.Sdk.Processor;
        using GraphQL;
        using Volo.Abp.Modularity;
        using Volo.Abp.ObjectMapping;
        using System;
        using System.Threading.Tasks;
        using System.Linq;
        using System.Collections.Generic;
        using Nest;
        using AeFinder.Sdk.Entities;
        using AElf.EntityMapping.Elasticsearch.Linq;
        using Newtonsoft.Json;
        using AutoMapper;
        using AutoMapper.Configuration;
        using System.Runtime.CompilerServices;
        using System.Numerics;
        using System.Text.RegularExpressions; 

        namespace TestApp;

        [NestedAttributes(""Test"")]
        public class TestAppEntity : AeFinderEntity, IAeFinderEntity
        {
            public int IntValue { get; set; }
            [Keyword]
            public string StringValue { get; set; }
            [Text]
            public string StringValue2 { get; set; }
        }
        
        public class TestAppEntityDto
        {
            public int IntValue { get; set; }
            public TimeSpan TimeSpan { get; set; }
            public BigInteger BigInteger {get; set;}
        }

        public class Query
        {
            public static async Task<List<TestAppEntityDto>> TokenInfo(
                [FromServices] IReadOnlyRepository<TestAppEntity> repository,
                [FromServices] IObjectMapper objectMapper, string chainId)
            {
                var query = await repository.GetQueryableAsync();
                query = query.After(new object[]{1,100});
                var list = query.ToList();

                return objectMapper.Map<List<TestAppEntity>, List<TestAppEntityDto>>(list);
            }

            public static bool Match(string source, string pattern)
            {
                return source.IsNullOrEmpty() ? false : Regex.IsMatch(source, pattern);
            }
        }

        public class TestAppSchema : AppSchema<Query>
        {
            protected TestAppSchema(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
        }

        public class TestAppBlockProcessor : BlockProcessorBase
        {
            public override async Task ProcessAsync(AeFinder.Sdk.Processor.Block block, BlockContext context)
            {
                var json = JsonConvert.SerializeObject(block);
                var time = DateTimeOffset.FromUnixTimeMilliseconds(1).UtcDateTime;

                var sets = new HashSet<string>();
                var list = new List<string>();
                var address = list.RemoveAll(o => sets.Contains(o));
                var day = DateTime.Now.DayOfWeek;
                var tuple = Tuple.Create(1);
                var tuple1 = new Tuple<int>(1);
                var tuple2 = new Tuple<int, int>(1, 1);
                var tuple3 = new Tuple<int, int, int>(1, 1, 1);
                var tuple4 = new Tuple<int, int, int, int>(1, 1, 1, 1);
                var tuple5 = new Tuple<int, int, int, int, int>(1, 1, 1, 1, 1);
                var tuple6 = new Tuple<int, int, int, int, int, int>(1, 1, 1, 1, 1, 1);
                var tuple7 = new Tuple<int, int, int, int, int, int, int>(1, 1, 1, 1, 1, 1, 1);
                var tuple8 = new Tuple<int, int, int, int, int, int, int, Tuple<int>>(1, 1, 1, 1, 1, 1, 1, new Tuple<int>(4));
                        
                throw new NotImplementedException();
                throw new SwitchExpressionException();
                throw new ArgumentException();
            }
        }

        public class TestAutoMapperProfile : AutoMapper.Profile
        {
            public TestAutoMapperProfile()
            {
                CreateMap<TestAppEntityDto, TestAppEntity>()
                    .ForMember(destination => destination.IntValue, opt => opt.MapFrom(source => source.IntValue))
                    .ForPath(destination => destination.IntValue, opt => opt.MapFrom(source => source.IntValue));
            }
        }

        public class TestAppModule:AbpModule
        {
            public override void ConfigureServices(ServiceConfigurationContext context)
            {
            }
        }

        public class TestAppConst
        {
            public static readonly List<string> List = [""a"", ""b"", ""c""];
        }
        ";
        AddAssemblies(typeof(FromServicesAttribute).Assembly.Location, typeof(IObjectMapper).Assembly.Location,
            typeof(AbpModule).Assembly.Location, typeof(KeywordAttribute).Assembly.Location,
            typeof(AElfEntityMappingModule).Assembly.Location,
            typeof(AElfEntityMappingElasticsearchModule).Assembly.Location,
            typeof(JsonConvert).Assembly.Location,
            typeof(AutoMapper.Profile).Assembly.Location);
        var assemblyDefinition = CompileToAssemblyDefinition(sourceCode);

        var validationResult = _whitelistValidator.Validate(assemblyDefinition.MainModule, CancellationToken.None);
        validationResult.Count().ShouldBe(0);
    }

    [Fact]
    public void Validate_Failed_Test()
    {
        var sourceCode = @"
        using AeFinder.Sdk;
        using AeFinder.Sdk.Processor;
        using Nest;
        using System.Net.Http;
        using Microsoft.Extensions.Logging;
        using System.Threading.Tasks;
        using System;
        using AeFinder.Sdk.Entities;

        namespace TestApp;

        public class TestAppEntity : AeFinderEntity, IAeFinderEntity
        {
            public int IntValue { get; set; }
            [Keyword]
            public string StringValue { get; set; }
            public DateAttribute T {get;set;}
        }

        public class Client
        {
            public HttpClient GetClient()
            {
                return new HttpClient();
            }
        }

        public class TestAppBlockProcessor : BlockProcessorBase
        {
            private readonly ILogger<TestAppBlockProcessor> _logger;

            public override async Task ProcessAsync(AeFinder.Sdk.Processor.Block block, BlockContext context)
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