using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Ems.ExamManagement.Data;

/* This is used if database provider does't define
 * IExamManagementDbSchemaMigrator implementation.
 */
public class NullExamManagementDbSchemaMigrator : IExamManagementDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
