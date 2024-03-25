using Volo.Abp.Settings;

namespace AeFinder.Settings;

public class AeFinderSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(AeFinderSettings.MySetting1));
    }
}
