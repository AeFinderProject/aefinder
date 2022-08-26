using Volo.Abp.Settings;

namespace AElfScan.Settings;

public class AElfScanSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(AElfScanSettings.MySetting1));
    }
}
