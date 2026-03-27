using System.Threading.Tasks;

namespace Ems.ExamExecution.Data;

public interface IExamExecutionDbSchemaMigrator
{
    Task MigrateAsync();
}
