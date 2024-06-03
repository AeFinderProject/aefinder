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

    // protected override void CreateModel(IMongoModelBuilder modelBuilder)
    // {
    //     base.CreateModel(modelBuilder);
    //
    //     //modelBuilder.Entity<YourEntity>(b =>
    //     //{
    //     //    //...
    //     //});
    // }
    
    public IMongoCollection<IdentityUserOrganizationUnit> IdentityUserOrganizationUnit { get; set; }

    protected override void CreateModel(IMongoModelBuilder modelBuilder)
    {
        base.CreateModel(modelBuilder);
            
        // Configure entities in MongoDB
        modelBuilder.Entity<IdentityUserOrganizationUnit>(b =>
        {
            b.CollectionName = "IdentityUserOrganizationUnit";
        });
    }
}
