using Volo.Abp.Modularity;

namespace Ems.ExamManagement;

[DependsOn(
    typeof(ExamManagementDomainModule),
    typeof(ExamManagementTestBaseModule)
)]
public class ExamManagementDomainTestModule : AbpModule
{

}
