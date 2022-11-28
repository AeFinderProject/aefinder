using Volo.Abp.Settings;

namespace AElfIndexer.Settings;

public class AElfIndexerSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(AElfIndexerSettings.MySetting1));
    }
}
