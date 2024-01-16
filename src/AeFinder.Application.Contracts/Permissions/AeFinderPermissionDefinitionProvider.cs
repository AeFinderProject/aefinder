using AeFinder.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace AeFinder.Permissions;

public class AeFinderPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(AeFinderPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(AeFinderPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<AeFinderResource>(name);
    }
}
