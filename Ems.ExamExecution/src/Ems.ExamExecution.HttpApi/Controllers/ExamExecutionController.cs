using Ems.ExamExecution.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace Ems.ExamExecution.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class ExamExecutionController : AbpControllerBase
{
    protected ExamExecutionController()
    {
        LocalizationResource = typeof(ExamExecutionResource);
    }
}
