using AElfIndexer.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace AElfIndexer.Permissions;

public class AElfIndexerPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(AElfIndexerPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(AElfIndexerPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<AElfIndexerResource>(name);
    }
}
