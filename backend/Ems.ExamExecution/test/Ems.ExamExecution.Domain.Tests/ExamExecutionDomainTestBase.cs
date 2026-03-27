using Volo.Abp.Modularity;

namespace Ems.ExamExecution;

/* Inherit from this class for your domain layer tests. */
public abstract class ExamExecutionDomainTestBase<TStartupModule> : ExamExecutionTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
