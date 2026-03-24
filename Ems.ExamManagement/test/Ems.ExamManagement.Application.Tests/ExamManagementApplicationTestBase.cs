using Volo.Abp.Modularity;

namespace Ems.ExamManagement;

public abstract class ExamManagementApplicationTestBase<TStartupModule> : ExamManagementTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
