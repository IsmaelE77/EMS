using Volo.Abp.Modularity;

namespace Ems.ExamManagement;

[DependsOn(
    typeof(ExamManagementApplicationModule),
    typeof(ExamManagementDomainTestModule)
)]
public class ExamManagementApplicationTestModule : AbpModule
{

}
