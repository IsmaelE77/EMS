using Ems.ExamManagement.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace Ems.ExamManagement.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(ExamManagementEntityFrameworkCoreModule),
    typeof(ExamManagementApplicationContractsModule)
    )]
public class ExamManagementDbMigratorModule : AbpModule
{
}
