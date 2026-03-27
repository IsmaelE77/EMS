using Microsoft.Extensions.Localization;
using Ems.ExamManagement.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace Ems.ExamManagement;

[Dependency(ReplaceServices = true)]
public class ExamManagementBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<ExamManagementResource> _localizer;

    public ExamManagementBrandingProvider(IStringLocalizer<ExamManagementResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
