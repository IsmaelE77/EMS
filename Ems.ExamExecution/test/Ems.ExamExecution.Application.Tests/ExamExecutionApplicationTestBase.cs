using Volo.Abp.Modularity;

namespace Ems.ExamExecution;

public abstract class ExamExecutionApplicationTestBase<TStartupModule> : ExamExecutionTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
