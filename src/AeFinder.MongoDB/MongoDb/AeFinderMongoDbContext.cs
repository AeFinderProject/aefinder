using AeFinder.App;
using AeFinder.User;
using MongoDB.Driver;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.MongoDB;

namespace AeFinder.MongoDb;

[ConnectionStringName("Default")]
public class AeFinderMongoDbContext : AbpMongoDbContext
{
    /* Add mongo collections here. Example:
     * public IMongoCollection<Question> Questions => Collection<Question>();
     */
    public IMongoCollection<IdentityUserExtension> IdentityUserExtensionInfos { get; private set; }
    public IMongoCollection<OrganizationUnitExtension> OrganizationUnitExtensionInfos { get; private set; }
    protected override void CreateModel(IMongoModelBuilder modelBuilder)
    {
        base.CreateModel(modelBuilder);
    
        //modelBuilder.Entity<YourEntity>(b =>
        //{
        //    //...
        //});
        
        
        modelBuilder.Entity<IdentityUserExtension>(b =>
        {
            b.CollectionName = "IdentityUserExtensions"; 
        });
        modelBuilder.Entity<OrganizationUnitExtension>(b =>
        {
            b.CollectionName = "OrganizationUnitExtensions"; 
        });
    }
    
}
