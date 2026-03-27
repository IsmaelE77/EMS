using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Ems.ExamExecution.Data;

/* This is used if database provider does't define
 * IExamExecutionDbSchemaMigrator implementation.
 */
public class NullExamExecutionDbSchemaMigrator : IExamExecutionDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
