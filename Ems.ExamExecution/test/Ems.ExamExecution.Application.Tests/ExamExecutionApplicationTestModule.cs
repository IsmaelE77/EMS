using Volo.Abp.Modularity;

namespace Ems.ExamExecution;

[DependsOn(
    typeof(ExamExecutionApplicationModule),
    typeof(ExamExecutionDomainTestModule)
)]
public class ExamExecutionApplicationTestModule : AbpModule
{

}
