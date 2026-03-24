using Ems.ExamExecution.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Ems.ExamExecution.Permissions;

public class ExamExecutionPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(ExamExecutionPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(ExamExecutionPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<ExamExecutionResource>(name);
    }
}
