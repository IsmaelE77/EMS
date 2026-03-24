using Volo.Abp.Settings;

namespace Ems.ExamExecution.Settings;

public class ExamExecutionSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(ExamExecutionSettings.MySetting1));
    }
}
