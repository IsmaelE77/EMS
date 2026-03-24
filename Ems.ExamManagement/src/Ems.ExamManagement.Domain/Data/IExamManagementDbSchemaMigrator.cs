using System.Threading.Tasks;

namespace Ems.ExamManagement.Data;

public interface IExamManagementDbSchemaMigrator
{
    Task MigrateAsync();
}
