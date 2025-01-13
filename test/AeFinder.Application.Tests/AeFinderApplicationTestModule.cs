using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.App.Deploy;
using AeFinder.Apps;
using AeFinder.Assets;
using AeFinder.CodeOps;
using AeFinder.Grains.Grain.ApiKeys;
using AeFinder.Grains.Grain.Assets;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.Grain.Users;
using AeFinder.Grains.Grain.Merchandises;
using AeFinder.Grains.State.Assets;
using AeFinder.Grains.State.Merchandises;
using AeFinder.Merchandises;
using AeFinder.Grains.Grain.Users;
using AeFinder.Metrics;
using AeFinder.Options;
using AeFinder.Orleans.TestBase;
using AeFinder.User;
using AElf.EntityMapping.Elasticsearch;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Emailing;
using Orleans;
using Volo.Abp;
using Volo.Abp.Emailing;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Threading;

namespace AeFinder;

[DependsOn(
    typeof(AeFinderApplicationModule),
    typeof(AeFinderDomainTestModule),
    typeof(AeFinderOrleansTestBaseModule),
    typeof(AElfEntityMappingElasticsearchModule)
)]
public class AeFinderApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<ApiOptions>(o =>
        {
            o.BlockQueryHeightInterval = 1000;
            o.TransactionQueryHeightInterval = 1000;
            o.LogEventQueryHeightInterval = 1000;
            o.MaxQuerySize = 10;
        });
        
        context.Services.AddTransient<ICodeAuditor>(o=>Mock.Of<ICodeAuditor>());

        context.Services.Configure<BlockPushOptions>(o =>
        {
            o.MessageStreamNamespaces = new List<string> { "MessageStreamNamespace" };
        });
        context.Services.AddTransient<IAppResourceLimitProvider, AppResourceLimitProvider>();
        context.Services.AddTransient<IKubernetesAppMonitor, DefaultKubernetesAppMonitor>();
        context.Services.AddHttpClient();
        context.Services.Configure<ApiKeyOptions>(o =>
        {
            o.IgnoreKeys = new HashSet<string> { "app" };
        });
        context.Services.Configure<UserRegisterOptions>(o => { o.EmailSendingInterval = 0; });
        context.Services.Configure<AwsEmailOptions>(o =>
        {
            o.From = "sam@XXXX.com";
            o.ConfigSet = "MockConfigSet";
            o.FromName = "MockName";
            o.Host = "MockHost";
            o.Image = "MockImage";
            o.Port = 8000;
            o.SmtpUsername = "MockUsername";
            o.SmtpPassword = "MockPassword";
        });
        
        context.Services.AddSingleton<IEmailSender, NullEmailSender>();
        context.Services.Configure<EmailTemplateOptions>(o =>
        {
            o.Templates = new Dictionary<string, EmailTemplate>
            {
                {
                    AeFinderApplicationConsts.RegisterEmailTemplate, new EmailTemplate
                    {
                        Body = "Body",
                        IsBodyHtml = false,
                        Subject = "Subject",
                        From = "From"
                    }
                }
            };
        });
        context.Services.Configure<CustomOrganizationOptions>(c =>
        {
            c.CustomApps = new List<string>()
            {
                "appid"
            };
        });
        var mockGraphQLClient = new Mock<IGraphQLClient>();
        context.Services.AddSingleton<IGraphQLClient>(mockGraphQLClient.Object);

        context.Services.Configure<UserRegisterOptions>(o =>
        {
            o.EmailSendingInterval = 0;
        });

        context.Services.AddSingleton<IEmailSender, NullEmailSender>();
        context.Services.Configure<EmailTemplateOptions>(o =>
        {
            o.Templates = new Dictionary<string, EmailTemplate>
            {
                { AeFinderApplicationConsts.RegisterEmailTemplate, new EmailTemplate
                {
                    Body = "Body",
                    IsBodyHtml = false,
                    Subject = "Subject",
                    From = "From"
                } }
            };
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var clusterClient = context.ServiceProvider.GetRequiredService<IClusterClient>();
        var merchandiseService = context.ServiceProvider.GetRequiredService<IMerchandiseService>();
        var assetService = context.ServiceProvider.GetRequiredService<IAssetService>();
        var objectMapper = context.ServiceProvider.GetRequiredService<IObjectMapper>();

        AsyncHelper.RunSync(async () =>
            await InitMerchandisesAsync(clusterClient, merchandiseService, assetService, objectMapper));
    }

    private async Task InitMerchandisesAsync(IClusterClient clusterClient,IMerchandiseService merchandiseService,IAssetService assetService,IObjectMapper objectMapper)
    {
        var merchandises = new Dictionary<Guid, CreateMerchandiseInput>();
        var apiQueryId = Guid.NewGuid();
        var apiQueryInput = new CreateMerchandiseInput
        {
            Name = "ApiQuery",
            Price = 0.00004M,
            ChargeType = ChargeType.Time,
            Category = MerchandiseCategory.Query,
            Type = MerchandiseType.ApiQuery,
            Status = MerchandiseStatus.Listed,
            SortWeight = 1
        };
        merchandises[apiQueryId] = apiQueryInput;
        
        var processorSmallId = Guid.NewGuid();
        var processorSmallInput = new CreateMerchandiseInput
        {
            Name = "Small",
            Price = 1,
            ChargeType = ChargeType.Hourly,
            Category = MerchandiseCategory.Resource,
            Type = MerchandiseType.Processor,
            Status = MerchandiseStatus.Listed,
            SortWeight = 2
        };
        merchandises[processorSmallId] = processorSmallInput;
        
        var processorMiddleId = Guid.NewGuid();
        var processorMiddleInput = new CreateMerchandiseInput
        {
            Name = "Middle",
            Price = 2,
            ChargeType = ChargeType.Hourly,
            Category = MerchandiseCategory.Resource,
            Type = MerchandiseType.Processor,
            Status = MerchandiseStatus.Listed,
            SortWeight = 3
        };
        merchandises[processorMiddleId] = processorMiddleInput;
        
        var processorLargeId = Guid.NewGuid();
        var processorLargeInput = new CreateMerchandiseInput
        {
            Name = "Large",
            Price = 3,
            ChargeType = ChargeType.Hourly,
            Category = MerchandiseCategory.Resource,
            Type = MerchandiseType.Processor,
            Status = MerchandiseStatus.Listed,
            SortWeight = 4
        };
        merchandises[processorLargeId] = processorLargeInput;
        
        var storageId = Guid.NewGuid();
        var storageInput = new CreateMerchandiseInput
        {
            Name = "Storage",
            Price = 0.1M,
            ChargeType = ChargeType.Hourly,
            Category = MerchandiseCategory.Resource,
            Type = MerchandiseType.Storage,
            Status = MerchandiseStatus.Listed,
            SortWeight = 5
        };
        merchandises[storageId] = storageInput;

        foreach (var merchandise in merchandises)
        {
            var grain = clusterClient.GetGrain<IMerchandiseGrain>(merchandise.Key);
            var state = await grain.CreateAsync(merchandise.Key, merchandise.Value);
            await merchandiseService.AddOrUpdateIndexAsync(
                objectMapper.Map<MerchandiseState, MerchandiseChangedEto>(state));
        }
        
        var apiAssetId = Guid.NewGuid();
        var assetGrain = clusterClient.GetGrain<IAssetGrain>(apiAssetId);
        await assetGrain.CreateAssetAsync(apiAssetId, AeFinderApplicationTestConsts.OrganizationId,
            new CreateAssetInput
            {
                MerchandiseId = apiQueryId,
                Quantity = 100000,
                Replicas = 1,
                FreeQuantity = 100000,
                FreeReplicas = 1,
                FreeType = AssetFreeType.Permanent,
                CreateTime = DateTime.UtcNow
            });
        await assetGrain.StartUsingAsync(DateTime.UtcNow);
        var assetState = await assetGrain.GetAsync();
        await assetService.AddOrUpdateIndexAsync(
            objectMapper.Map<AssetState, AssetChangedEto>(assetState));
    }
}
