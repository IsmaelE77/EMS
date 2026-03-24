using Volo.Abp.Modularity;

namespace Ems.ExamExecution;

[DependsOn(
    typeof(ExamExecutionDomainModule),
    typeof(ExamExecutionTestBaseModule)
)]
public class ExamExecutionDomainTestModule : AbpModule
{

}
