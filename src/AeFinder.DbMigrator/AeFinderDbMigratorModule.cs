﻿using AeFinder.MongoDb;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Modularity;
using Volo.Abp.Identity;
using Volo.Abp.Identity.MongoDB;
using IdentityRole = Volo.Abp.Identity.IdentityRole;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace AeFinder.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AeFinderMongoDbModule),
    typeof(AeFinderApplicationContractsModule),
    typeof(AbpIdentityDomainModule),
    typeof(AbpIdentityMongoDbModule)
    )]
public class AeFinderDbMigratorModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpBackgroundJobOptions>(options => options.IsJobExecutionEnabled = false);

        IdentityBuilderExtensions.AddDefaultTokenProviders(context.Services.AddIdentity<IdentityUser, IdentityRole>());
        // .AddEntityFrameworkStores<ApplicationDbContext>()
    }
}
