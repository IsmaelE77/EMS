using System;
using System.Collections.Generic;
using System.Text;
using Ems.ExamExecution.Localization;
using Volo.Abp.Application.Services;

namespace Ems.ExamExecution;

/* Inherit your application services from this class.
 */
public abstract class ExamExecutionAppService : ApplicationService
{
    protected ExamExecutionAppService()
    {
        LocalizationResource = typeof(ExamExecutionResource);
    }
}
