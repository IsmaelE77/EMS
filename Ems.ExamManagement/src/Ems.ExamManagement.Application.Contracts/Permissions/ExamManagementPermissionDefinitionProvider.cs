using Ems.ExamManagement.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Ems.ExamManagement.Permissions;

public class ExamManagementPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(ExamManagementPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(ExamManagementPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<ExamManagementResource>(name);
    }
}
