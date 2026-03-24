using Volo.Abp.Settings;

namespace Ems.ExamManagement.Settings;

public class ExamManagementSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(ExamManagementSettings.MySetting1));
    }
}
