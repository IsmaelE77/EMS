using Ems.ExamManagement.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace Ems.ExamManagement.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class ExamManagementController : AbpControllerBase
{
    protected ExamManagementController()
    {
        LocalizationResource = typeof(ExamManagementResource);
    }
}
