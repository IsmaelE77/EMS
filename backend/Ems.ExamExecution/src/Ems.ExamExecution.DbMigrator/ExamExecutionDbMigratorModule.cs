using Ems.ExamExecution.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace Ems.ExamExecution.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(ExamExecutionEntityFrameworkCoreModule),
    typeof(ExamExecutionApplicationContractsModule)
    )]
public class ExamExecutionDbMigratorModule : AbpModule
{
}
