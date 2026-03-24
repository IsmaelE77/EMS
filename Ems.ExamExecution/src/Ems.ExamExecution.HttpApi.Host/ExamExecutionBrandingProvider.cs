using Microsoft.Extensions.Localization;
using Ems.ExamExecution.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace Ems.ExamExecution;

[Dependency(ReplaceServices = true)]
public class ExamExecutionBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<ExamExecutionResource> _localizer;

    public ExamExecutionBrandingProvider(IStringLocalizer<ExamExecutionResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
