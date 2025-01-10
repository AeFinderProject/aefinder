using System.Collections.Generic;
using AeFinder.App.Deploy;
using AeFinder.Apps;
using AeFinder.CodeOps;
using AeFinder.Grains.Grain.ApiKeys;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.Grain.Users;
using AeFinder.Metrics;
using AeFinder.Orleans.TestBase;
using AeFinder.User;
using AElf.EntityMapping.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Emailing;
using Volo.Abp.Modularity;

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
        
        context.Services.Configure<ApiKeyOptions>(o =>
        {
            o.IgnoreKeys = new HashSet<string> { "app" };
        });

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
}
