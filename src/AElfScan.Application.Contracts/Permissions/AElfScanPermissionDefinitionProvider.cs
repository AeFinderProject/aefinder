using AElfScan.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace AElfScan.Permissions;

public class AElfScanPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(AElfScanPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(AElfScanPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<AElfScanResource>(name);
    }
}
