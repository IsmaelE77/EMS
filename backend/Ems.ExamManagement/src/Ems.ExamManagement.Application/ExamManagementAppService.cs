using System;
using System.Collections.Generic;
using System.Text;
using Ems.ExamManagement.Localization;
using Volo.Abp.Application.Services;

namespace Ems.ExamManagement;

/* Inherit your application services from this class.
 */
public abstract class ExamManagementAppService : ApplicationService
{
    protected ExamManagementAppService()
    {
        LocalizationResource = typeof(ExamManagementResource);
    }
}
